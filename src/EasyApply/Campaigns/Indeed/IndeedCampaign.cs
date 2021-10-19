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
            : base(configuration) { }

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

            var login = WebDriver.FindElement(By.Id(Constants.IndeedLoginCssID));
            login.SendKeys(Configuration.OpportunityConfiguration.Username);

            var pass = WebDriver.FindElement(By.Id(Constants.IndeedPasswordCssID));
            pass.SendKeys(Configuration.OpportunityConfiguration.Password);
            
            // note : need user interaction to bypass captcha
            Console.WriteLine("Hit any key after login & captcha");
            Console.ReadKey();
        }

        /// <summary>
        /// Search Indeed to get search landing page
        /// </summary>
        private void GetSearchPage()
        {
            var uri = String.Format("{0}/jobs?q={1}&l={2}",
                Constants.IndeedUrl,
                HttpUtility.UrlEncode(Configuration.OpportunityConfiguration.Position),
                HttpUtility.UrlEncode(Configuration.OpportunityConfiguration.Location));

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
                        IndeedOpportunity opportunity = null;
                                               
                        // check for easy apply
                        var easilyApply = this.ParseEasyResumeTags(container);

                        // parse html for opportunity info
                        opportunity = await this.ParseOpportunityContainer(container, easilyApply);

                        if (opportunity.Applied)
                        {
                            Console.WriteLine($"[*] Applied (sync) : {opportunity.Company} - {opportunity.Position}");
                            continue;
                        }

                        // check for easy apply apply if opportunity is easy apply
                        if (easilyApply)
                        {
                            // apply for for opportunity 
                            opportunity = await this.SubmitEasyApply(opportunity);
                        }
                           
                        if(opportunity != null)
                        {
                            Console.WriteLine($"[*] Applied : {opportunity.Company} - {opportunity.Position}");
                        }
                    }
                }

                // move to next page
                pagination++;
                var next = doc.DocumentNode.SelectSingleNode(Constants.IndeedNextSearchLink).GetAttributeValue("href", string.Empty);
                WebDriver.Url = Constants.IndeedUrl + next;
                Console.WriteLine($"[*] {pagination} pages scraped, {opportunities} opportunities parsed");
            }
        }

        /// <summary>
        /// Checks job for easy apply tags
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private bool ParseEasyResumeTags(HtmlNode container)
        {
            if(container.SelectSingleNode(Constants.IndeedXpathEasilyApply) != null) return true;
            else if (container.SelectNodes(Constants.IndeedXpathEasyResumeButton) != null) return true;
            else return false;
        }

        /// <summary>
        /// Parse and create opportunity link
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private string ParseOpportunityLink(HtmlNode node)
        {
            return String.Format("https://www.indeed.com/viewjob?jk={0}&tk={1}",
                node.Attributes["data-jk"].Value,
                node.Attributes["data-mobtk"].Value);
        }

        /// <summary>
        /// Scrape opportunity containers for parsing
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private HtmlNodeCollection GetOpportunityContainers(HtmlDocument document)
        {
            return document.DocumentNode.SelectNodes(Constants.IndeedContainerCssClass);
        }

        /// <summary>
        /// Parse and create opportunity object
        /// </summary>
        /// <param name="node"></param>
        /// <param name="easyApply"></param>
        /// <returns></returns>
        private async Task<IndeedOpportunity> ParseOpportunityContainer(HtmlNode node, bool easyApply)
        {
            // syncronize local database with indeed, skipp
            var opportunity = new IndeedOpportunity();
            opportunity.Created = DateTime.Now;
            opportunity.Position = node.SelectSingleNode(Constants.IndeedXpathJobTitle)?.InnerText;
            opportunity.Company = node.SelectSingleNode(Constants.IndeedXpathCompany)?.InnerText;
            opportunity.Location = node.SelectSingleNode(Constants.IndeedXpathLocation)?.InnerText;
            opportunity.Salary = node.SelectSingleNode(Constants.IndeedXpathSalary)?.InnerText;
            opportunity.Description = node.SelectSingleNode(Constants.IndeedXpathJobSnippet)?.InnerText;

            // create link from jk/tk job data
            opportunity.Link = this.ParseOpportunityLink(node);
            opportunity.Applied = (node.SelectSingleNode(Constants.IndeedXpathAppliedAlready) != null) ? true : false;

            if(!await DataRepository.CheckIndeedOpportunity(opportunity.Link) && opportunity.Applied)
            {
                await DataRepository.AddIndeedOpportunity(opportunity);
            }

            opportunity.EasyApply = easyApply;
            return opportunity;
        }

        /// <summary>
        /// Submit an easily apply opportunity
        /// </summary>
        /// <param name="opportunity"></param>
        /// <returns></returns>
        private async Task<IndeedOpportunity> SubmitEasyApply(IndeedOpportunity opportunity)
        {
            try
            {
                // open new window and switch to handle
                ((IJavaScriptExecutor) WebDriver).ExecuteScript($"window.open('{opportunity.Link}', '_blank')");
                WebDriver.SwitchTo().Window(WebDriver.WindowHandles[1]);

                // click to apply
                var wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
                wait.Until(x => x.FindElement(By.XPath(Constants.IndeedXpathApplyButton))).Click();

                // WebDriver.FindElement(By.XPath(Constants.IndeedXpathApplyButton)).Click();

                try
                {
                    WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
                    // check for applied opportunity
                    WebDriver.FindElement(By.XPath(Constants.IndeedXpathAppliedAlready));

                    Console.WriteLine($"[*] Applied (sync): {opportunity.Company} - {opportunity.Position}");

                    if(!await DataRepository.CheckIndeedOpportunity(opportunity.Link))
                    {
                        await DataRepository.AddIndeedOpportunity(opportunity);
                    }

                    opportunity = null;
                }
                catch (NoSuchElementException)
                {
                    // auto apply loop
                    bool isParsing = true;
                    while (isParsing)
                    {
                        // check for application view
                        isParsing = AutoApplyStepCheck();
                    }

                    // mark opportunity applied
                    opportunity.Applied = true;
                }
                finally
                {
                    WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
                }
            }
            catch (Exception ex)
            {
                if (Program.VerboseMode)
                {
                    Debug.WriteLine($"Easy apply exception : {ex.ToString()}");
                }
                    
                opportunity = null;
            }
            finally
            {
                // close application window & return to main search page
                if (WebDriver.WindowHandles.Count > 0)
                    WebDriver.Close();

                WebDriver.SwitchTo().Window(WebDriver.WindowHandles[0]);
            }

            return opportunity;
        }

        /// <summary>
        /// Step check for inputting form values
        /// </summary>
        /// <returns></returns>
        private bool AutoApplyStepCheck()
        {
            var heading = WebDriver.FindElement(By.XPath(Constants.IndeedXpathHeader)).Text;
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

        /// <summary>
        /// Submit custom PDF to application
        /// </summary>
        private void ApplyResumeStep()
        {
            var wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
            wait.Until(x => x.FindElement(By.XPath(Constants.IndeedXpathPdfResume))).Click();

            ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");
            var wait1 = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
            wait1.Until(x => x.FindElement(By.XPath(Constants.IndeedXpathContinueButton))).Click();
        }

        /// <summary>
        /// Submit employee questions
        /// </summary>
        private void ApplyQuestionsStep1()
        {
            // locate questions container
            // var questionsContainer = WebDriver.FindElement(By.XPath(Constants.IndeedXpathQuestionContainer));

            // get questions from container
            var questions = WebDriver.FindElements(By.XPath(Constants.IndeedXpathQuestions));

            // loop over questions to answer 
            foreach (var element in questions)
            {
                ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollBy(0,25)", "");

                // get input id, question, and possible answer from yml configuration
                var inputId = element.FindElement(By.XPath(Constants.IndeedXpathInputId)).GetAttribute("for");
                var question = element.FindElement(By.XPath(Constants.IndeedXpathLabelQuestionValue)).Text;
                var answer = Configuration.OpportunityQuestions.FirstOrDefault(x => question.ToLower().Contains(x.Substring.ToLower()));

                try
                {
                    WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1);
 
                    // check for radio button 
                    var radioInput = element.FindElement(By.XPath(Constants.IndeedXpathRadioFieldset));

                    // var radioInput = element.FindElement(By.XPath(Constants.IndeedXpathRadioFieldset));
                    var labelInputs = radioInput.FindElements(By.XPath(Constants.IndeedXpathLabelInputs));

                    var index = 1;
                    bool selected = false;
                    // loop over radio options for value to match answer
                    foreach(var label in labelInputs)
                    {
                        if (selected) continue;

                        var option = label.FindElement(By.XPath(Constants.IndeedXpathOptionValue)).Text;
                        if(answer != null && option.ToLower() == answer.Answer.ToLower() || option == "Yes")
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
                }
                catch (NoSuchElementException)
                {
                    WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);

                    // check for input or textarea for question input 
                    foreach (var inputType in new List<string>() { "input", "textarea" })
                    {
                        try
                        {
                            // find input
                            var input = element.FindElement(By.XPath($".//{inputType}[contains(@id, {inputId})]"));
                            if (answer != null)
                            {
                                if (string.IsNullOrEmpty(input.GetAttribute("value")))
                                {
                                    // enter answer
                                    input.Clear();
                                    input.SendKeys(answer.Answer);
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[*] New question : {question}");
                                // enter default answer
                                input.Clear();
                                input.SendKeys("1");
                            }
                        }
                        catch (NoSuchElementException) { }
                    }
                }
            }

            ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");
            WebDriver.FindElement(By.XPath(Constants.IndeedXpathContinueButton)).Click();
        }

        private void ApplyQuestionsStep()
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(WebDriver.PageSource);

            // locate questions container
            // var questionsContainer = doc.DocumentNode.SelectSingleNode(Constants.IndeedXpathQuestionContainer);

            // get questions from container
            var questions = doc.DocumentNode.SelectNodes(Constants.IndeedXpathQuestions);

            // loop over questions to answer 
            foreach (var element in questions)
            {
                ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollBy(0,100)", "");

                // get input id, question, and possible answer from yml configuration
                var inputId = element.SelectSingleNode(Constants.IndeedXpathInputId).GetAttributeValue("for", string.Empty);
                var question = element.SelectSingleNode(Constants.IndeedXpathLabelQuestionValue);    
                var answer = Configuration.OpportunityQuestions
                    .FirstOrDefault(x => question.InnerText.IndexOf(x.Substring, 0, StringComparison.CurrentCultureIgnoreCase) != -1);

                // check for radio buttons 
                var radioInput = element.SelectSingleNode(Constants.IndeedXpathRadioFieldset);
                if(radioInput != null)
                {
                    var labelInputs = radioInput.SelectNodes(Constants.IndeedXpathLabelInputs);

                    // loop over radio options for value to match answer
                    int index = 0;
                    bool selected = false;
                    foreach(var label in labelInputs)
                    {
                        if (selected) continue;

                        // check radio button for answer options
                        var option = label.SelectSingleNode(Constants.IndeedXpathOptionValue).InnerText; 
                        if(option.IndexOf(answer.Answer, 0, StringComparison.CurrentCultureIgnoreCase) != -1
                            || option.ToLower() == "yes")
                        {
                            // select radio button if answer matches or yes
                            var radioButton = WebDriver.FindElement(By.Id($"{inputId}-{index}"));
                            if (!radioButton.Selected)
                            {
                                // radioButton.Click();
                                ((IJavaScriptExecutor)WebDriver).ExecuteScript("arguments[0].click();", radioButton);
                            }
                            selected = true;
                        }
                        index++;
                    }

                    if (!selected)
                    {
                        WebDriver.FindElement(By.Id($"{inputId}-2")).Click();
                    }
                }
                else
                {
                    // find input
                    var input = WebDriver.FindElement(By.Id(inputId));
                    var type = input.GetAttribute("type");
                    if (answer != null)
                    {
                        input.Clear();
                        input.SendKeys(answer.Answer);
                    }
                    else
                    {
                        Console.WriteLine($"[*] New question : {question.InnerText}");
                        if (!question.OuterHtml.Contains("optional"))
                        {
                            input.Clear();
                            input.SendKeys("1");
                        }
                    }
                }
            }

            ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");
            var wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
            wait.Until(x => x.FindElement(By.XPath(Constants.IndeedXpathContinueButton))).Click();
        }

        /// <summary>
        /// Skip auto populated job experience step
        /// </summary>
        private void ApplyPastJobExperienceStep()
        {
            // auto populated
            var wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
            wait.Until(x => x.FindElement(By.XPath(Constants.IndeedXpathContinueButton))).Click();
        }

        /// <summary>
        /// Bypass employer qualification requirements
        /// </summary>
        private void ApplyBypassRequiredQualifications()
        {
            ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");
            var wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
            wait.Until(x => x.FindElement(By.XPath(Constants.IndeedXpathContinueButton))).Click();
        }

        /// <summary>
        /// Add custom or default cover letter
        /// </summary>
        private void ApplyAddCoverLetterStep()
        {
            try
            {
                // add resume located at yml configuration path
                var wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
                var uploadFile = wait.Until(x => x.FindElement(By.Id("additionalDocuments")));
                uploadFile.SendKeys(Configuration.OpportunityConfiguration.CoverLetter);
            }
            catch (NoSuchElementException)
            {
                // select auto populated cover letter text
                var wait1 = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
                wait1.Until(x => x.FindElement(By.XPath("//div[contains(@id, 'write-cover-letter-selection-card')]"))).Click();
            }

            ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");
            var wait2 = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
            wait2.Until(x => x.FindElement(By.XPath(Constants.IndeedXpathContinueButton))).Click();
        }

        /// <summary>
        /// Review, finalize & submit application
        /// </summary>
        private void ApplyReviewStep()
        {
            ((IJavaScriptExecutor) WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");
            var wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
            wait.Until(x => x.FindElement(By.XPath(Constants.IndeedXpathContinueButton))).Click();
        }
    }
}