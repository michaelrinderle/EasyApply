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

using EasyApply.Models;
using OpenQA.Selenium;

namespace EasyApply
{
    /// <summary>
    /// Constants
    /// </summary>
    public static class Constants
    {
        // Environment Variables 
        public static readonly string ProgramFiles86 = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");
        public static readonly string ProgramFiles64 = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
        public static readonly string LocalAppData = Environment.ExpandEnvironmentVariables("%LocalAppData%");
        public static readonly string ProfileDesktop = Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Desktop");

        // Indeed Urls
        public static readonly string IndeedUrl = "https://www.indeed.com";
        public static readonly string IndeedLoginUrl = "https://www.indeed.com/account/login";

        // Indeed CSS Xpaths
        public static readonly By IndeedLoginCssID = By.Id("login-email-input");
        public static readonly By IndeedPasswordCssID = By.Id("login-password-input");
        public static readonly By IndeedWhatCssID = By.Id("text-input-what");
        public static readonly By IndeedWhereCssID = By.Id("text-input-where");

        public static readonly string IndeedContainerCssClass = "//a[contains(@class, 'result')]";

        public static readonly string IndeedXpathEasilyApply = ".//span[text()='Easily apply']";
        public static readonly string IndeedXpathApplyButton = ".//button[@id='indeedApplyButton']";
        public static readonly string IndeedXpathJobTitle = ".//h2/span";
        public static readonly string IndeedXpathCompany = ".//pre/span[@class='companyName']";
        public static readonly string IndeedXpathLocation = ".//div[@class='companyLocation']";
        public static readonly string IndeedXpathSalary = ".//span[@class= 'salary-snippet']";
        public static readonly string IndeedXpathJobSnippet = ".//div[@class='job-snippet']/ul/li";
        public static readonly string IndeedNextSearchLink = "//a[@aria-label='Next']";

        // Indeed Application Headers & Xpaths
        public static readonly string IndeedResumeHeader = "Add a resume";
        public static readonly string IndeedQuestionsHeader = "Questions from";
        public static readonly string IndeedPastExperienceHeader = "Enter a past job";
        public static readonly string IndeedQualificationsHeader = "is looking for these qualifications";
        public static readonly string IndeedCoverLetterHeader = "Want to include any supporting documents";
        public static readonly string IndeedReviewHeader = "Please review your application";


        public static readonly string IndeedXpathAppliedAlready = "//h1[contains(@class, 'ia-HasApplied-bodyTop--text')]";
        public static readonly string IndeedXpathPdfResume = "//div[contains(@class, 'resume-display-container')]";
        public static readonly string IndeedXpathContinueButton = "//button[contains(@class, 'ia-continueButton')]";
        public static readonly string IndeedXpathQuestionContainer = "//div[contains(@class, 'ia-BasePage-component')]";
        public static readonly string IndeedXpathQuestions = "//div[contains(@class, 'ia-Questions-item')]";
        public static readonly string IndeedXpathInputId = ".//div/label";
        public static readonly string IndeedXpathLabelQuestionValue = ".//div/label/span";
        public static readonly string IndeedXpathRadioFieldset = ".//div/fieldset";
        public static readonly string IndeedXpathLabelInputs = ".//label";
        public static readonly string IndeedXpathOptionValue = ".//span[2]";


        public static readonly List<JobQuestions> JobQuestions = new List<JobQuestions>()
        {
            new JobQuestions()
            {
               Substring = "What is the highest level of education",
               Answer = "High School or equivalent"
            },
            new JobQuestions()
            {
               Substring = "Will you be able to reliably commute or relocate",
               Answer = "Yes, but I need relocation assistance"
            },
            new JobQuestions()
            {
               Substring = "Do you have an active security clearance",
               Answer = "No"
            },
            new JobQuestions()
            {
               Substring = "Are you authorized to work in the United States",
               Answer = "Yes"
            },
            new JobQuestions()
            {
               Substring = "What are your salary requirements",
               Answer = "> $75K"
            },
            new JobQuestions()
            {
               Substring = " Will you, now or in the future require visa",
               Answer = "No"
            },       
            new JobQuestions()
            {
               Substring = "How many years of Front-end development",
               Answer = "8"
            },
            new JobQuestions()
            {
               Substring = "How many years of React",
               Answer = "2"
            },
            new JobQuestions()
            {
               Substring = "How many years of JavaScript",
               Answer = "8"
            },
            new JobQuestions()
            {
               Substring = "How many years of Node.js",
               Answer = "2"
            },
            new JobQuestions()
            {
               Substring = "Please list 2-3 dates and time ranges that you could do an interview.",
               Answer = "Monday-Friday 11-6PM CST"
            },
        };

        // Dependency Paths
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
    }
}

//div[contains(@class, 'file-question-upload-button')]
