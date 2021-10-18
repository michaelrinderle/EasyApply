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

using EasyApply.Factories;
using EasyApply.Interfaces;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace EasyAppy
{
    class Program
    {
        public static ICampaign? Campaign { get; set; }
        public static bool VerboseMode { get; set; } = false;

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int Main(string[] args)
        {
            return CreateRootCommand().InvokeAsync(args).Result;
        }

        /// <summary>
        /// Sets up command line flags & options
        /// </summary>
        /// <returns></returns>
        private static RootCommand CreateRootCommand()
        {
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    new string[]{ "--path", "-p"},
                    description: "Path to campaign yml file"),
                new Option<bool>(
                    new string[]{ "--verbose", "-v"},
                    description: "verbose mode")
            };

            rootCommand.Description = "EasyApply";
            rootCommand.Handler = CommandHandler.Create<string, bool>(LaunchCampaign);

            return rootCommand;
        }

        /// <summary>
        /// Launches job campaign
        /// </summary>
        /// <param name="path"></param>
        /// <param name="verbose"></param>
        private static async void LaunchCampaign(string path, bool verbose)
        {
            Console.WriteLine("[*] EasyApply v1.0");
            Console.WriteLine("[*] Written by the Michael Rinderle <michael@sofdigital.net>\n");

            VerboseMode = verbose;

            Campaign = new CampaignFactory().CreateCampaign(path);
            await Campaign.StartCampaign();
        }
    }
}