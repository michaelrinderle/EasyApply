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

using EasyApply.Campaigns.Indeed;
using EasyApply.Models;
using EasyApply.Repositories;

namespace EasyApply.Interfaces
{
    /// <summary>
    /// Data dependency injection class
    /// </summary>
    public class IData : IDataRepository
    {
        private readonly IDataRepository _dataRepository;

        public IData(IDataRepository dataRepository)
        {
            _dataRepository = dataRepository;
            _dataRepository.InitializeDatabase();
        }

        public void InitializeDatabase()
        {
            _dataRepository.InitializeDatabase();
        }

        public async Task<IndeedOpportunity> AddIndeedOpportunity(IndeedOpportunity indeedOpportunity)
        {
            return await _dataRepository.AddIndeedOpportunity(indeedOpportunity);
        }

        public async Task<IndeedOpportunity> GetIndeedOpportunity(int id)
        {
            return await _dataRepository.GetIndeedOpportunity(id);
        }

        public async Task<List<IndeedOpportunity>> GetIndeedOpportunities()
        {
            return await _dataRepository.GetIndeedOpportunities();
        }

        public async Task<bool> CheckIndeedOpportunity(string link)
        {
            return await _dataRepository.CheckIndeedOpportunity(link);
        }

        public async Task<bool> UpdateIndeedOpportunity(IndeedOpportunity indeedOpportunity)
        {
            return await _dataRepository.UpdateIndeedOpportunity(indeedOpportunity);
        }

        public async Task<MonsterOpportunity> AddMonsterOpportunity(MonsterOpportunity MonsterOpportunity)
        {
            return await _dataRepository.AddMonsterOpportunity(MonsterOpportunity);
        }

        public async Task<MonsterOpportunity> GetMonsterOpportunity(int id)
        {
            return await _dataRepository.GetMonsterOpportunity(id);
        }

        public async Task<List<MonsterOpportunity>> GetMonsterOpportunities()
        {
            return await _dataRepository.GetMonsterOpportunities();
        }

        public async Task<bool> CheckMonsterOpportunity(string link)
        {
            return await _dataRepository.CheckMonsterOpportunity(link);
        }

        public async Task<bool> UpdateMonsterOpportunity(MonsterOpportunity MonsterOpportunity)
        {
            return await _dataRepository.UpdateMonsterOpportunity(MonsterOpportunity);
        }
    }
}
