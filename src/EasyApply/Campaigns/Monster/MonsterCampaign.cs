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

using EasyApply.Enums;
using EasyApply.Interfaces;
using EasyApply.Models;
using EasyAppy;
using HtmlAgilityPack;
using OpenQA.Selenium;
using System.Diagnostics;
using System.Web;

namespace EasyApply.Campaigns.Monster
{
    public class MonsterCampaign : ICampaign
    {
        private static OpportunityCounter Counter = new();

        public MonsterCampaign(YmlConfiguration configuration)
            : base(configuration) { }

        public override async Task StartCampaign()
        {
            this.GetLogin();
            this.GetSearchPage(null);
            await this.StartJobSearch();
        }

        public override void GetLogin()
        {
            // browser profile should have indeed cookies for bypass
            if (!string.IsNullOrEmpty(Configuration.Browser.BrowserProfile)) return;

            // login to indeed
            WebDriver.Url = Constants.MonsterLoginUrl;

            var login = WebDriver.FindElement(By.Id(Constants.MonsterLoginCssID));
            login.SendKeys(Configuration.OpportunityConfiguration.Username);

            var pass = WebDriver.FindElement(By.Id(Constants.MonsterPasswordCssID));
            pass.SendKeys(Configuration.OpportunityConfiguration.Password);

            pass.SendKeys(Keys.Enter);

            // note : need user interaction to bypass captcha
            //Console.WriteLine("Hit any key after login & captcha");
            //Console.ReadKey();
        }

        public override void GetSearchPage(int? page)
        {
            // encode indeed search url string
            var uri = String.Format("{0}/jobs/search?q={1}&where={2}",
                Constants.MonsterUrl,
                HttpUtility.UrlEncode(Configuration.OpportunityConfiguration.Position),
                HttpUtility.UrlEncode(Configuration.OpportunityConfiguration.Location));

            if (page != null)
                uri = uri + $"&page={page}";

            WebDriver.Url = uri;
        }

        public override async Task StartJobSearch()
        {
            while (true)
            {
                Thread.Sleep(3000);
                var doc = this.GetPageSource();
                // get containers from search landing page
                var containers = this.GetOpportunityContainers(doc);
                foreach (var container in containers)
                {
                    await this.ParseOpportunityContainer(container);
                    ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, 1000)");
                }

                Counter.Pagination++;

                //((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 1000)");
                //Thread.Sleep(4000);

                Console.WriteLine($"[*] Pages Scraped :{Counter.Pagination} \n[*] Opportunities Parsed : {Counter.Opportunities}"); 
            }
        }

        public HtmlDocument GetPageSource()
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(WebDriver.PageSource);
            return doc;
        }

        private HtmlNodeCollection GetOpportunityContainers(HtmlDocument doc)
        {
            var opportunityContainer = doc.DocumentNode.SelectSingleNode(Constants.MonsterXpathJobResults);
            return opportunityContainer.SelectNodes(Constants.MonsterXpathJobCard);
        }

        private async Task ParseOpportunityContainer(HtmlNode node)
        {
            try
            {
                // check if opportunity is in database or not null
                var opportunityLink = node.Attributes["href"].Value.Replace("//", "");
                if (!await DataRepository.CheckMonsterOpportunity(opportunityLink))
                {
                    Counter.Opportunities++;

                    ((IJavaScriptExecutor)WebDriver).ExecuteScript($"window.open('{opportunityLink}', 'NewTab')");
                    WebDriver.SwitchTo().Window(WebDriver.WindowHandles[1]);

                    var doc = this.GetPageSource();

                    // parse opportunity
                    var opportunity = this.ParseOpportunity(doc, opportunityLink);

                    // go to opportunity page to determine if applied
                    var status = this.ApplyForOpportunity();
                    switch (status)
                    {
                        case ApplyType.Applied:
                            {
                                Counter.AppliedTo++;
                                Counter.EasyAppliedTo++;
                                opportunity.Applied = true;
                                Console.WriteLine($"[*][{Counter.EasyAppliedTo,3}] Applied:\t{opportunity.Company} - {opportunity.Position}");
                                break;
                            }
                        case ApplyType.AlreadyApplied:
                            {
                                Counter.AppliedTo++;
                                opportunity.Applied = true;
                                Console.WriteLine($"[*][{Counter.AppliedTo,3}] Synced:\t{opportunity.Company} - {opportunity.Position}");
                                break;
                            }
                        case ApplyType.External:
                            {
                                Counter.Saved++;
                                Console.WriteLine($"[*][{ Counter.Saved,3}] Saved:\t\t{opportunity.Company} - {opportunity.Position}");
                                break;
                            }
                        case ApplyType.Exception:
                            {
                                Console.WriteLine($"[*][X] Exception:\t\t{opportunity.Company} - {opportunity.Position}");
                                break;
                            }
                    }

                    await DataRepository.AddMonsterOpportunity(opportunity);
                }


            }
            catch (Exception ex)
            {
                if (Program.VerboseMode)
                {
                    Debug.WriteLine($"Easy apply exception : {ex.ToString()}");
                }
            }
        }

        private MonsterOpportunity ParseOpportunity(HtmlDocument doc, string opportunityLink)
        {
            var opportunity = new MonsterOpportunity();
            opportunity.Link = opportunityLink;
            opportunity.Position = doc.DocumentNode.SelectSingleNode(".//h1[contains(@class, 'headerstyle__JobViewHeaderTitle')]").InnerHtml;
            opportunity.Company = doc.DocumentNode.SelectSingleNode(".//h2[contains(@class, 'headerstyle__JobViewHeaderCompany')]").InnerHtml;
            opportunity.Location = doc.DocumentNode.SelectSingleNode(".//h3[contains(@class, 'headerstyle__JobViewHeaderLocation')]").InnerHtml;

            var salary = doc.DocumentNode.SelectSingleNode(".//div[contains(@class, 'descriptionstyles__DescriptionBody')]/div/b[2]/following-sibling::text()[1]");
            if (salary != null)
            {
                opportunity.Salary = salary.InnerText;
            }

            var description = doc.DocumentNode.SelectSingleNode(".//div[contains(@class, 'descriptionstyles__DescriptionBody')]/div/b[2]/following-sibling::text()[2]");
            if (description != null)
            {
                opportunity.Description = description.InnerText;
            }

            return opportunity;
        }

        private ApplyType ApplyForOpportunity()
        {
            try
            {
                // go to opportunity page to determine if applied
                WebDriver.FindElement(By.XPath(Constants.MonsterXpathApplyButton)).Click();
                WebDriver.SwitchTo().Window(WebDriver.WindowHandles[2]);

                Thread.Sleep(4000);

                var doc = this.GetPageSource();
                var internalApply = WebDriver.Url.Contains("monster.com");
                var applied = doc.DocumentNode.SelectSingleNode(Constants.MonsterXpathAppliedElement);

                // confirm opportunity questions are correct
                if (internalApply && applied == null) // || applied.InnerText != "Your application has been sent!")
                {
                    try
                    {
                        if (doc.DocumentNode.SelectSingleNode(Constants.MonsterXpathFirstName).Attributes["value"] == null)
                        {
                            var element = WebDriver.FindElement(By.XPath(Constants.MonsterXpathFirstName));
                            ((IJavaScriptExecutor)WebDriver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
                            element.SendKeys(Configuration.MonsterConfiguration.FirstName);
                        }
                        if (doc.DocumentNode.SelectSingleNode(Constants.MonsterXpathLastName).Attributes["value"] == null)
                        {
                            var element = WebDriver.FindElement(By.XPath(Constants.MonsterXpathLastName));
                            ((IJavaScriptExecutor)WebDriver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
                            element.SendKeys(Configuration.MonsterConfiguration.LastName);
                        }
                        if (doc.DocumentNode.SelectSingleNode(Constants.MonsterXpathPronoun).Attributes["value"] == null)
                        {
                            var element = WebDriver.FindElement(By.XPath(Constants.MonsterXpathPronoun));
                            ((IJavaScriptExecutor)WebDriver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
                            element.SendKeys(Configuration.MonsterConfiguration.Pronouns);
                        }
                        if (doc.DocumentNode.SelectSingleNode(Constants.MonsterXpathPhoneNumber).Attributes["value"] == null)
                        {
                            var element = WebDriver.FindElement(By.XPath(Constants.MonsterXpathPhoneNumber));
                            ((IJavaScriptExecutor)WebDriver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
                            element.SendKeys(Configuration.MonsterConfiguration.PhoneNumber);
                        }

                        // todo: add functionality to choice select option

                        //if (doc.DocumentNode.SelectSingleNode(Constants.MonsterXpathPhoneType).Attributes["value"] == null)
                        //{
                        //    var element = WebDriver.FindElement(By.XPath(Constants.MonsterXpathPhoneType));
                        //    ((IJavaScriptExecutor)WebDriver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
                        //    element.SendKeys(Configuration.MonsterConfiguration.PhoneType);
                        //}

                        if (doc.DocumentNode.SelectSingleNode(Constants.MonsterXpathCountry).Attributes["value"] == null)
                        {
                            var element = WebDriver.FindElement(By.XPath(Constants.MonsterXpathCountry));
                            ((IJavaScriptExecutor)WebDriver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
                            element.SendKeys(Configuration.MonsterConfiguration.Country);
                        }
                        if (doc.DocumentNode.SelectSingleNode(Constants.MonsterXpathPostalCode).Attributes["value"] == null)
                        {
                            var element = WebDriver.FindElement(By.XPath(Constants.MonsterXpathPostalCode));
                            ((IJavaScriptExecutor)WebDriver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
                            element.SendKeys(Configuration.MonsterConfiguration.PostalCode);
                        }
                        if (doc.DocumentNode.SelectSingleNode(Constants.MonsterXpathRegion).Attributes["value"] == null)
                        {
                            var element = WebDriver.FindElement(By.XPath(Constants.MonsterXpathRegion));
                            ((IJavaScriptExecutor)WebDriver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
                            element.SendKeys(Configuration.MonsterConfiguration.Region);
                        }
                        if (doc.DocumentNode.SelectSingleNode(Constants.MonsterXpathCity).Attributes["value"] == null)
                        {
                            var element = WebDriver.FindElement(By.XPath(Constants.MonsterXpathCity));
                            ((IJavaScriptExecutor)WebDriver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
                            element.SendKeys(Configuration.MonsterConfiguration.PhoneNumber);
                        }

                        ((IJavaScriptExecutor)WebDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight - 150)");

                        WebDriver.FindElement(By.XPath(".//button[@role='button'][normalize-space()='SUBMIT']")).Click();

                        Thread.Sleep(1000);
                        return ApplyType.Applied;
                    }
                    catch (Exception ex)
                    {
                        if (Program.VerboseMode)
                        {
                            Debug.WriteLine($"Easy apply exception : {ex.ToString()}");
                        }
                        return ApplyType.Exception;
                    }
                }

                WebDriver.Close();
                WebDriver.SwitchTo().Window(WebDriver.WindowHandles[1]);

                if (!internalApply) return ApplyType.External;
                else if (applied != null) return ApplyType.AlreadyApplied;
                else return ApplyType.Exception;
            }
            catch (Exception ex)
            {
                if (Program.VerboseMode)
                {
                    Debug.WriteLine($"Easy apply exception : {ex.ToString()}");
                }
                return ApplyType.Exception;
            }
            finally
            {
                WebDriver.Close();
                WebDriver.SwitchTo().Window(WebDriver.WindowHandles[0]);
            }
        }
    }
}