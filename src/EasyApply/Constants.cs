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

namespace EasyApply
{
    /// <summary>
    /// Constants
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Environment Variables
        /// </summary>

        public static readonly string ProgramFiles86 = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");

        public static readonly string ProgramFiles64 = Environment.ExpandEnvironmentVariables("%ProgramW6432%");

        public static readonly string LocalAppData = Environment.ExpandEnvironmentVariables("%LocalAppData%");

        public static readonly string ProfileDesktop = Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Desktop");

        /// <summary>
        /// Dependency Checks
        /// </summary>
        
        public static readonly string DefaultCampaignPath = "easyapply.yml";

        public static readonly string DefaultLogPath = $"{ProfileDesktop}\\easyapply.logs";

        public static readonly string[] GeckDriverPaths = new[]
        {
            "C:\\tools\\selenium\\geckodriver.exe",
        };

        public static readonly string[] FirefoxBrowserPaths = new[]
        {
            $"{ProgramFiles64}\\Mozilla Firefox\\firefox.exe",
            $"{ProgramFiles86}\\Mozilla Firefox\\firefox.exe",
        };

        public static readonly string[] ChromeBrowserPaths = new[]
        {
            $"{ProgramFiles64}\\Google\\Chrome\\Application\\chrome.exe",
            $"{ProgramFiles86}\\Google\\Chrome\\Application\\chrome.exe",
            $"{LocalAppData}\\Google\\Chrome\\Application\\chrome.exe",
        };

        /// <summary>
        /// Indeed Urls
        /// </summary>
        
        public static readonly string IndeedUrl = "https://www.indeed.com";

        public static readonly string IndeedLoginUrl = "https://www.indeed.com/account/login";

        public static readonly string IndeedLoginCssID = "login-email-input";

        public static readonly string IndeedPasswordCssID = "login-password-input";

        /// <summary>
        /// Indeed Search Xpaths
        /// </summary>

        public static readonly string IndeedContainerCssClass = "//a[contains(@class, 'result')]";

        public static readonly string IndeedXpathAppliedTag = ".//div[@id='applied-snippet']";

        public static readonly string IndeedXpathApplyButton = ".//button[@id='indeedApplyButton']";

        public static readonly string IndeedXpathEasilyApply = ".//span[text()='Easily apply']";

        public static readonly string IndeedXpathEasyResumeButton = ".//span[starts-with(., 'Apply with your Indeed Resume')]";

        public static readonly string IndeedXpathJobTitle = ".//h2/span";

        public static readonly string IndeedXpathCompany = ".//pre/span[@class='companyName']";

        public static readonly string IndeedXpathLocation = ".//div[@class='companyLocation']";

        public static readonly string IndeedXpathSalary = ".//span[@class= 'salary-snippet']";

        public static readonly string IndeedXpathJobSnippet = ".//div[@class='job-snippet']/ul/li";

        public static readonly string IndeedNextSearchLink = "//a[@aria-label='Next']";

        /// <summary>
        /// Indeed Application Headers
        /// </summary>
        
        public static readonly string IndeedResumeHeader = "Add a resume";

        public static readonly string IndeedQuestionsHeader = "Questions from";

        public static readonly string IndeedPastExperienceHeader = "past job";

        public static readonly string IndeedQualificationsHeader = "qualifications";

        public static readonly string IndeedCoverLetterHeader = "supporting documents";

        public static readonly string IndeedReviewHeader = "Please review your application";

        /// <summary>
        /// Indeed Application Xpaths
        /// </summary>
       
        public static readonly string IndeedXpathAppliedAlready = "//h1[contains(@class, 'ia-HasApplied-bodyTop--text')]";

        public static readonly string IndeedXpathHeader = "//h1[contains(@class, 'ia-BasePage-heading')]";

        public static readonly string IndeedXpathPdfResume = "//div[contains(@class, 'resume-display-container')]";

        public static readonly string IndeedXpathContinueButton = "//button[contains(@class, 'ia-continueButton')]";

        public static readonly string IndeedXpathQuestions = "//div[contains(@class, 'ia-Questions-item')]";

        public static readonly string IndeedXpathInputId = ".//div/label";

        public static readonly string IndeedXpathLabelQuestionValue = ".//div/label/span";

        public static readonly string IndeedXpathRadioFieldset = ".//div/fieldset";

        public static readonly string IndeedXpathLabelInputs = ".//label";

        public static readonly string IndeedXpathOptionValue = ".//span[2]";        
    }
}