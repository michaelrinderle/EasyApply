﻿/*

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
using YamlDotNet.Serialization;

namespace EasyApply.Parsers
{
    /// <summary>
    /// Yml configuration parser
    /// </summary>
    public class YmlConfigurationParser
    {
        public YmlConfigurationParser() { }

        /// <summary>
        /// Load user yml configuration file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static YmlConfiguration LoadConfiguration(string path)
        {
            string yml;
            if (string.IsNullOrEmpty(path))
            {
                if (File.Exists(Constants.DefaultCampaignPath))
                {
                    yml = File.ReadAllText(Constants.DefaultCampaignPath);
                    if (string.IsNullOrEmpty(yml))
                    {
                        Console.WriteLine("[*] Error parsing default configuration, Aborting");
                        Environment.Exit(1);
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine("[*] Could not find default or supplied configuration, Aborting");
                    Environment.Exit(1);
                    return null;
                }
            }
            else
                yml = File.ReadAllText(path);

            var deserializer = new DeserializerBuilder().Build();
            return deserializer.Deserialize<YmlConfiguration>(yml);
        }
    }
}
