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
            this.GetSearchPage(null);
            await this.StartJobSearch();
        }

        /// <summary>
        /// Log into Indeed if profile is not set with existing cookies
        /// </summary>
        public override void GetLogin()
        {
            // browser profile should have indeed cookies for bypass
            if (!string.IsNullOrEmpty(Configuration.Browser.BrowserProfile)) return;

            // login to indeed
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
        public override void GetSearchPage(int? page)
        {
            // encode indeed search url string
            var uri = String.Format("{0}/jobs?q={1}&l={2}",
                Constants.IndeedUrl,
                HttpUtility.UrlEncode(Configuration.OpportunityConfiguration.Position),
                HttpUtility.UrlEncode(Configuration.OpportunityConfiguration.Location));

            WebDriver.Url = uri;
        }

        /// <summary>
        /// Start job search
        /// </summary>
        public override async Task StartJobSearch()
        {
            OpportunityCounter counter = new();
            while (true)
            {
                // load page source
                var doc = new HtmlDocument();
                doc.LoadHtml(WebDriver.PageSource);

                // close popup if present
                var popup = doc.DocumentNode.SelectSingleNode(".//button[contains(@class, 'popover-x-button-close')]");
                if (popup != null)
                {
                    WebDriver.FindElement(By.XPath(".//button[contains(@class, 'popover-x-button-close')]")).Click();
                }

                // get job containers from search page
                var jobContainers = this.GetOpportunityContainers(doc);
                foreach (var container in jobContainers)
                {
                    // check for opportunity in database
                    if (!await DataRepository.CheckIndeedOpportunity(this.ParseOpportunityLink(container)))
                    {
                        counter.Opportunities++;
                        IndeedOpportunity opportunity = null;

                        // parse html for opportunity info
                        opportunity = await this.ParseOpportunityContainer(container);
                        if (opportunity.Id == -1)
                        {
                            Console.WriteLine($"[*] Rejected : \t{opportunity.Company} - {opportunity.Position}");
                        }
                        else if (!opportunity.Applied && opportunity.EasyApply)
                        {
                            // submit an easy apply applicaiton
                            opportunity = await this.SubmitEasyApply(opportunity);
                            // null on error, reporting from exception
                            if (opportunity == null) continue;

                            counter.EasyAppliedTo++;
                            counter.AppliedTo++;
                            Console.WriteLine($"[*][{counter.EasyAppliedTo,3}] Applied:\t{opportunity.Company} - {opportunity.Position}");
                        }
                        else if (opportunity.Applied)
                        {
                            // already applied
                            counter.AppliedTo++;
                            Console.WriteLine($"[*][{counter.AppliedTo,3}] Synced:\t{opportunity.Company} - {opportunity.Position}");
                        }
                        else
                        {
                            // saved for off-site application
                            counter.Saved++;
                            Console.WriteLine($"[*][{ counter.Saved,3}] Saved:\t\t{opportunity.Company} - {opportunity.Position}");
                        }
                    }
                }

                // move to next page
                counter.Pagination++;
                ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");
                WebDriver.FindElement(By.XPath(Constants.IndeedNextSearchLink)).Click();
                Console.WriteLine($"[*] Pages Scraped :{counter.Pagination} \n[*] Opportunities Parsed : {counter.Opportunities}");
            }
        }

        /// <summary>
        /// Checks job for easy apply tags
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private bool ParseEasyResumeTags(HtmlNode container)
        {
            // check job container for easy apply tags
            if (container.SelectSingleNode(Constants.IndeedXpathEasilyApply) != null) return true;
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
            // parse/create opportunity link
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
        private async Task<IndeedOpportunity> ParseOpportunityContainer(HtmlNode node)
        {
            var opportunity = new IndeedOpportunity();
            opportunity.Created = DateTime.Now;
            opportunity.Position = node.SelectSingleNode(Constants.IndeedXpathJobTitle)?.InnerText;
            opportunity.Company = node.SelectSingleNode(Constants.IndeedXpathCompany)?.InnerText;
            opportunity.Location = node.SelectSingleNode(Constants.IndeedXpathLocation)?.InnerText;
            opportunity.Salary = node.SelectSingleNode(Constants.IndeedXpathSalary)?.InnerText;
            opportunity.Description = node.SelectSingleNode(Constants.IndeedXpathJobSnippet)?.InnerText;

            // black & white lists
            bool isBlackWhiteListed = false;

            if (Configuration.OpportunityConfiguration.CompanyBlacklist != null &&
                Configuration.OpportunityConfiguration.CompanyBlacklist.Any())
            {
                foreach (var keyword in Configuration.OpportunityConfiguration.CompanyBlacklist)
                {
                    if (opportunity.Company.Contains(keyword))
                        isBlackWhiteListed = true;
                }
            }

            if (Configuration.OpportunityConfiguration.Blacklist != null &&
                Configuration.OpportunityConfiguration.Blacklist.Any())
            {
                foreach (var keyword in Configuration.OpportunityConfiguration.Blacklist)
                {
                    if (opportunity.Position.Contains(keyword))
                        isBlackWhiteListed = true;

                    if (Configuration.OpportunityConfiguration.ListType == Enums.ListType.OpportunityTitleAndDescription)
                    {
                        if (!string.IsNullOrEmpty(opportunity.Description))
                        {
                            if (opportunity.Description.Contains(keyword))
                                isBlackWhiteListed = true;
                        }
                    }
                }
            }

            if (Configuration.OpportunityConfiguration.Whitelist != null &&
                Configuration.OpportunityConfiguration.Whitelist.Any())
            {
                foreach (var keyword in Configuration.OpportunityConfiguration.Whitelist)
                {
                    if (opportunity.Position.Contains(keyword))
                        isBlackWhiteListed = true;

                    if (Configuration.OpportunityConfiguration.ListType == Enums.ListType.OpportunityTitleAndDescription)
                    {
                        if (!string.IsNullOrEmpty(opportunity.Description))
                        {
                            if (opportunity.Description.Contains(keyword))
                                isBlackWhiteListed = true;
                        }
                    }
                }
            }

            if (isBlackWhiteListed)
            {
                opportunity.Id = -1;
                return opportunity;
            }

            // create link from jk/tk job data
            opportunity.Link = this.ParseOpportunityLink(node);
            opportunity.Applied = (node.SelectSingleNode(Constants.IndeedXpathAppliedAlready) != null) ? true : false;
            opportunity.EasyApply = this.ParseEasyResumeTags(node);

            // syncronize local database with indeed
            if (!await DataRepository.CheckIndeedOpportunity(opportunity.Link))
            {
                await DataRepository.AddIndeedOpportunity(opportunity);
            }

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
                ((IJavaScriptExecutor)WebDriver).ExecuteScript($"window.open('{opportunity.Link}', 'NewTab')");
                WebDriver.SwitchTo().Window(WebDriver.WindowHandles[1]);

                // check position &description for white and blacklist keywords

                bool reject = false;
                var jobDescription = WebDriver.FindElement(By.Id(Constants.IndeedJobDesriptionId)).Text.Split(" ");
                List<string> descriptionTokens = new();

                if (Configuration.OpportunityConfiguration.Blacklist != null)
                {
                    if (!descriptionTokens.Any())
                    {
                        descriptionTokens = WebDriver.FindElement(By.Id(Constants.IndeedJobDesriptionId)).Text.Split(" ").ToList();
                    }
                    if (Configuration.OpportunityConfiguration.Blacklist.Any())
                    {
                        if (!Configuration.OpportunityConfiguration.Blacklist
                       .Any(x => jobDescription.Contains(x)))
                        {
                            Console.WriteLine($"[*] Rejected : \t{opportunity.Company} - {opportunity.Position}");
                            reject = true;
                        }
                    }
                }

                if (Configuration.OpportunityConfiguration.Whitelist != null)
                {
                    descriptionTokens = WebDriver.FindElement(By.Id(Constants.IndeedJobDesriptionId)).Text.Split(" ").ToList();
                    if (Configuration.OpportunityConfiguration.Whitelist.Any())
                    {
                        if (!Configuration.OpportunityConfiguration.Whitelist
                        .Any(x => jobDescription.Contains(x)))
                        {
                            Console.WriteLine($"[*] Rejected : \t{opportunity.Company} - {opportunity.Position}");
                            reject = true;
                        }
                    }
                }

                if (reject)
                {
                    opportunity = null;
                    return opportunity;
                }

                // click to apply
                var wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(10));
                wait.Until(x => x.FindElement(By.XPath(Constants.IndeedXpathApplyButton))).Click();

                try
                {
                    // look for indeed bug, say applied after already applying
                    WebDriver.FindElement(By.XPath(Constants.IndeedXpathAppliedBugTag));
                    Debug.WriteLine($"Indeed applied arlready bug");
                    return opportunity;
                }
                catch (NoSuchElementException) { }

                try
                {
                    WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

                    // double check for applied opportunity
                    WebDriver.FindElement(By.XPath(Constants.IndeedXpathAppliedAlready));

                    opportunity = null;
                }
                catch (NoSuchElementException)
                {
                    // auto application apply loop
                    // note: each loop detects application steps
                    bool isParsing = true;
                    while (isParsing)
                    {
                        // check for application view
                        isParsing = AutoApplyStepCheck();
                    }

                    // mark opportunity applied
                    opportunity.Applied = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Easy apply exception : {ex.ToString()}");
                    if (Program.VerboseMode)
                    {
                        Debug.WriteLine($"Easy apply exception : {ex.ToString()}");
                    }
                }
                finally
                {
                    WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

                    if (opportunity != null)
                    {
                        await DataRepository.UpdateIndeedOpportunity(opportunity);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Easy apply exception : {ex.Message}");
                if (Program.VerboseMode)
                {
                    Debug.WriteLine($"Easy apply exception : {ex.Message}");
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
            // check page header for application step
            var heading = WebDriver.FindElement(By.XPath(Constants.IndeedXpathHeader)).Text;
            if (heading.Contains(Constants.IndeedResumeHeader)) ApplyResumeStep();
            else if (heading.Contains(Constants.IndeedQuestionsHeader)) ApplyQuestionsStep();
            else if (heading.Contains(Constants.IndeedPastExperienceHeader)) ApplyPastJobExperienceStep();
            else if (heading.Contains(Constants.IndeedQualificationsHeader)) ApplyBypassRequiredQualifications();
            else if (heading.Contains(Constants.IndeedCoverLetterHeader))
            {
                if (!Configuration.IndeedConfiguration.BypassRequirements) return false;
                ApplyAddCoverLetterStep();
            }
            else if (heading.Contains(Constants.IndeedReviewHeader))
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
            // picks uploaded pdf resume on indeed
            var wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
            wait.Until(x => x.FindElement(By.XPath(Constants.IndeedXpathPdfResume))).Click();

            ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");
            var wait1 = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
            wait1.Until(x => x.FindElement(By.XPath(Constants.IndeedXpathContinueButton))).Click();
        }

        /// <summary>
        /// Submit employee questions
        /// </summary>
        private void ApplyQuestionsStep()
        {
            // missed questions
            int missedQuestions = 0;

            // load page source
            var doc = new HtmlDocument();
            doc.LoadHtml(WebDriver.PageSource);

            // get questions from container
            var questions = doc.DocumentNode.SelectNodes(Constants.IndeedXpathQuestions);

            // loop over questions to answer
            foreach (var element in questions)
            {
                // get input id, question, and possible answer from yml configuration
                var inputIdElement = element.SelectSingleNode(Constants.IndeedXpathInputId);
                if (inputIdElement == null) continue;

                var inputId = inputIdElement.GetAttributeValue("for", string.Empty);

                // scroll to input id
                ((IJavaScriptExecutor)WebDriver).ExecuteScript("arguments[0].scrollIntoView(true);", WebDriver.FindElement(By.Id(inputId)));

                var question = element.SelectSingleNode(Constants.IndeedXpathLabelQuestionValue);

                var answer = Configuration.OpportunityQuestions
                    .FirstOrDefault(x => question.InnerText.IndexOf(x.Substring, 0, StringComparison.CurrentCultureIgnoreCase) != -1);

                if (answer == null)
                {
                    Console.WriteLine($"[*] No answer : {question.InnerText}");
                    missedQuestions++;
                    continue;
                }

                // check for radio buttons
                var radioInput = element.SelectSingleNode(Constants.IndeedXpathRadioFieldset);
                if (radioInput != null)
                {
                    var labelInputs = radioInput.SelectNodes(Constants.IndeedXpathLabelInputs);

                    // loop over radio options for value to match answer
                    int index = 0;
                    bool selected = false;
                    foreach (var label in labelInputs)
                    {
                        if (selected) continue;

                        // check radio button for answer options
                        var option = label.SelectSingleNode(Constants.IndeedXpathOptionValue).InnerText;
                        if (answer != null && option.IndexOf(answer.Answer, 0, StringComparison.CurrentCultureIgnoreCase) != -1)
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
                        missedQuestions++;
                        WebDriver.FindElement(By.Id($"input-{inputId}-0")).Click();
                    }
                }
                else
                {
                    // find select or input
                    var input = WebDriver.FindElement(By.Id(inputId));
                    var type = input.GetAttribute("type");
                    if (answer != null)
                    {
                        if (!type.Contains("select"))
                        {
                            input.Clear();
                        }
                        input.SendKeys(answer.Answer);

                        if (!type.Contains("select"))
                        {
                            input.SendKeys(Keys.Enter);
                        }
                    }
                    else
                    {
                        if (!element.OuterHtml.Contains("optional"))
                        {
                            if (!type.Contains("select"))
                            {
                                input.Clear();
                            }
                            missedQuestions++;
                        }
                    }
                }
            }

            if (missedQuestions > 0)
            {
                throw new Exception("Missed Questions, moving on but got new questions for database");
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
            // bypass warnings about qualifications
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
                WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
                var uploadFile = WebDriver.FindElement(By.Id("additionalDocuments"));
                uploadFile.SendKeys(Configuration.OpportunityConfiguration.CoverLetter);

                WebDriver.FindElement(By.XPath(Constants.IneedXpathWriteCoverLetterButton)).Click();
            }
            catch (NoSuchElementException)
            {
                // select auto populated cover letter text
                var wait1 = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
                wait1.Until(x => x.FindElement(By.XPath(Constants.IneedXpathWriteCoverLetterButton))).Click();
            }

            WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
            ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");
            var wait2 = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
            wait2.Until(x => x.FindElement(By.XPath(Constants.IndeedXpathContinueButton))).Click();
        }

        /// <summary>
        /// Review, finalize & submit application
        /// </summary>
        private void ApplyReviewStep()
        {
            ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");
            var wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
            wait.Until(x => x.FindElement(By.XPath(Constants.IndeedXpathContinueButton))).Click();
        }
    }
}