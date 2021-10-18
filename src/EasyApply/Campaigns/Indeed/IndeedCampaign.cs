/*

      __ _/| _/. _  ._/__ /
    _\/_// /_///_// / /_|/
               _/
    
    sof digital 2021
    written by michael rinderle <michael@sofdigital.net>
    
    mit license
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:
    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.

*/

using EasyApply.Interfaces;
using EasyApply.Models;
using EasyAppy;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using System.Web;

namespace EasyApply.Campaigns.Indeed
{
    public class IndeedCampaign : ICampaign
    {
        public IndeedCampaign(YmlConfiguration configuration)
            : base(configuration)
        {
            // WebDriver = new Dri
        }

        /// <summary>
        /// Start job search campaign
        /// </summary>
        public override async Task StartCampaign()
        {
            this.GetLogin();
            this.GetSearchPage();
            await this.StartJobSearch();
        }

        /// <summary>
        /// Log into Indeed if profile is not set with existing cookies
        /// </summary>
        private void GetLogin()
        {
            if (!string.IsNullOrEmpty(Configuration.Browser.BrowserProfile)) return;

            WebDriver.Url = Constants.IndeedLoginUrl;

            var login = WebDriver.FindElement(Constants.IndeedLoginCssID);
            login.SendKeys(Configuration.JobConfiguration.Username);

            var pass = WebDriver.FindElement(Constants.IndeedPasswordCssID);
            pass.SendKeys(Configuration.JobConfiguration.Password);

            Console.WriteLine("Hit any key after login & capture");
            Console.ReadKey();
        }

        /// <summary>
        /// Search Indeed to get search landing page
        /// </summary>
        private void GetSearchPage()
        {

            var uri = String.Format("{0}/jobs?q={1}&l={2}",
                Constants.IndeedUrl,
                HttpUtility.UrlEncode(Configuration.JobConfiguration.Position),
                HttpUtility.UrlEncode(Configuration.JobConfiguration.Location));

            WebDriver.Url = uri;
        }

        /// <summary>
        /// Start job search
        /// </summary>
        private async Task StartJobSearch()
        {
            int opportunities = 0;
            int pagination = 1;
            while (true)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(WebDriver.PageSource);

                // get job containers from search page
                var jobContainers = this.GetOpportunityContainers(doc);
                foreach (var container in jobContainers)
                {
                    // check for opportunity in database
                    if (!await DataRepository.CheckIndeedOpportunity(this.ParseOpportunityLink(container)))
                    {
                        opportunities++;

                        // check for easy apply
                        var easilyApply = container.SelectSingleNode(Constants.IndeedXpathEasilyApply);

                        // parse html for opportunity info
                        var opportunity = this.ParseOpportunityContainer(container, (easilyApply != null ? true : false));

                        // check for easy apply apply if opportunity is easy apply
                        if (easilyApply != null)
                        {
                            // apply for for opportunity 
                            opportunity = this.SubmitEasyApply(opportunity);
                        }

                        await DataRepository.AddIndeedOpportunity(opportunity);

                        Console.WriteLine($"[*] Applied : {opportunity.Company} - {opportunity.Position}");
                    }
                }

                // move to next page
                pagination++;
                var next = doc.DocumentNode.SelectSingleNode(Constants.IndeedNextSearchLink).GetAttributeValue("href", string.Empty);
                WebDriver.Url = next;
                Console.WriteLine($"[*] {pagination} pages scraped, {opportunities} opportunities parsed");
            }
        }

        private string ParseOpportunityLink(HtmlNode node)
        {
            return String.Format("https://www.indeed.com/viewjob?jk={0}&tk={1}",
                node.Attributes["data-jk"].Value,
                node.Attributes["data-mobtk"].Value);
        }

        private HtmlNodeCollection GetOpportunityContainers(HtmlDocument document)
        {
            return document.DocumentNode.SelectNodes(Constants.IndeedContainerCssClass);
        }

        private IndeedOpportunity ParseOpportunityContainer(HtmlNode node, bool easyApply)
        {
            var opportunity = new IndeedOpportunity();
            opportunity.Created = DateTime.Now;
            opportunity.Position = node.SelectSingleNode(Constants.IndeedXpathJobTitle)?.InnerText;
            opportunity.Company = node.SelectSingleNode(Constants.IndeedXpathCompany)?.InnerText;
            opportunity.Location = node.SelectSingleNode(Constants.IndeedXpathLocation)?.InnerText;
            opportunity.Salary = node.SelectSingleNode(Constants.IndeedXpathSalary)?.InnerText;
            opportunity.Description = node.SelectSingleNode(Constants.IndeedXpathJobSnippet)?.InnerText;

            // create link from jk/tk job data
            opportunity.Link = this.ParseOpportunityLink(node);

            opportunity.EasyApply = easyApply;
            return opportunity;
        }

        private IndeedOpportunity SubmitEasyApply(IndeedOpportunity opportunity)
        {
            try
            {
                // open new window and switch to handle
                ((IJavaScriptExecutor) WebDriver).ExecuteScript($"window.open('{opportunity.Link}', '_blank')");
                WebDriver.SwitchTo().Window(WebDriver.WindowHandles[1]);

                // click to apply
                WebDriver.FindElement(By.XPath(Constants.IndeedXpathApplyButton)).Click();

                try
                {
                    // check for applied opportunity
                    WebDriver.FindElement(By.XPath(Constants.IndeedXpathAppliedAlready));
                }
                catch (NoSuchElementException)
                {
                    bool isParsing = true;
                    while (isParsing)
                    {
                        isParsing = AutoApplyStepCheck();
                    }
                }

                opportunity.Applied = true;
            }
            catch (Exception ex)
            {
                if (Program.VerboseMode)
                {
                    if (Program.VerboseMode)
                    {
                        Debug.WriteLine($"Easy apply exception : {ex.ToString()}");
                    }
                }

                opportunity.Applied = false;
            }
            finally
            {
                if (WebDriver.WindowHandles.Count > 0)
                    WebDriver.Close();

                WebDriver.SwitchTo().Window(WebDriver.WindowHandles[0]);
            }

            return opportunity;
        }

        private bool AutoApplyStepCheck()
        {
            var heading = WebDriver.FindElement(By.XPath("//h1[contains(@class, 'ia-BasePage-heading')]")).Text;
            if(heading.Contains(Constants.IndeedResumeHeader)) ApplyResumeStep();
            else if(heading.Contains(Constants.IndeedQuestionsHeader)) ApplyQuestionsStep();
            else if(heading.Contains(Constants.IndeedPastExperienceHeader)) ApplyPastJobExperienceStep();
            else if (heading.Contains(Constants.IndeedQualificationsHeader)) ApplyBypassRequiredQualifications();
            else if (heading.Contains(Constants.IndeedCoverLetterHeader))  ApplyAddCoverLetterStep();
            else if(heading.Contains(Constants.IndeedReviewHeader))
            {
                ApplyReviewStep();
                return false;
            }

            return true;
        }

        private void ApplyResumeStep()
        {
            var customPdf = WebDriver.FindElement(By.XPath(Constants.IndeedXpathPdfResume));
            customPdf.Click();

            ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");

            WebDriver.FindElement(By.XPath(Constants.IndeedXpathContinueButton)).Click();
        }

        private void ApplyQuestionsStep()
        {
            var questionsContainer = WebDriver.FindElement(By.XPath(Constants.IndeedXpathQuestionContainer));
            var questions = questionsContainer.FindElements(By.XPath(Constants.IndeedXpathQuestions));

            foreach (var element in questions)
            {        
                var inputId = element.FindElement(By.XPath(Constants.IndeedXpathInputId)).GetAttribute("for");
                var question = element.FindElement(By.XPath(Constants.IndeedXpathLabelQuestionValue)).Text;
                var answer = Constants.JobQuestions.FirstOrDefault(x => question.Contains(x.Substring));

                try
                {
                    // check for radio 
                    var wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(1));
                    var radioInput = wait.Until(d => d.FindElement(By.XPath(Constants.IndeedXpathRadioFieldset)));

                    // var radioInput = element.FindElement(By.XPath(Constants.IndeedXpathRadioFieldset));
                    var labelInputs = radioInput.FindElements(By.XPath(Constants.IndeedXpathLabelInputs));

                    var index = 1;
                    bool selected = false;
                    foreach(var label in labelInputs)
                    {
                        var option = label.FindElement(By.XPath(Constants.IndeedXpathOptionValue)).Text;
                        if(option == answer.Answer)
                        {
                            var radioButton = label.FindElement(By.XPath($".//input"));  // [@id = {inputId}-{index}]
                            if (!radioButton.Selected)
                                radioButton.Click();
                            selected = true;
                        }
                        index++;
                    }

                    if (!selected)
                    {
                        radioInput.FindElement(By.XPath($".//input[@id = {inputId}-{2}]")).Click();
                    }

                    continue;
                }
                catch (NoSuchElementException ex)
                {
                    foreach(var inputType in new List<string>() { "input", "textarea" })
                    {
                        try
                        {
                            var input = element.FindElement(By.XPath($".//{inputType}[contains(@id, {inputId})]"));
                            if (answer != null)
                            {
                                input.Clear();
                                input.SendKeys(answer.Answer);
                            }
                            else
                                input.SendKeys("8");
                        }
                        catch (NoSuchElementException) { }
                    }
                }
            }

            ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");
            WebDriver.FindElement(By.XPath(Constants.IndeedXpathContinueButton)).Click();
        }

        private void ApplyPastJobExperienceStep()
        {
            // auto populated
            WebDriver.FindElement(By.XPath(Constants.IndeedXpathContinueButton)).Click();
        }

        private void ApplyBypassRequiredQualifications()
        {
            ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");
            WebDriver.FindElement(By.XPath(Constants.IndeedXpathContinueButton)).Click();
        }

        private void ApplyAddCoverLetterStep()
        {
            try
            {
                var uploadFile = WebDriver.FindElement(By.Id("additionalDocuments"));
                uploadFile.SendKeys(Configuration.JobConfiguration.CoverLetter);
            }
            catch (NoSuchElementException)
            {
                // write cover letter 
                WebDriver.FindElement(By.XPath("//div[contains(@id, 'write-cover-letter-selection-card')]")).Click();
            }

            ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");
            WebDriver.FindElement(By.XPath(Constants.IndeedXpathContinueButton)).Click();
        }

        private void ApplyReviewStep()
        {
            ((IJavaScriptExecutor) WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");
            WebDriver.FindElement(By.XPath(Constants.IndeedXpathContinueButton)).Click();
        }
    }
}