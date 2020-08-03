using Balances;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Singluar.Infrastructure.Factories;
using Singluar.Utilities.Enums;
using System.Collections.Generic;
using System.Net;

namespace Singluar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameBalanceController : ControllerBase
    {
        private readonly BalanceFactory _factory;

        public GameBalanceController(BalanceFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Gets current game balance
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("balance")]
        [ProducesResponseType(typeof(IEnumerable<decimal>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public ActionResult<decimal> Get()
        {
            return _factory.GetManagerService(BalanceFactoryType.Game).GetBalance();
        }

        /// <summary>
        /// Checks transaction status
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <returns></returns>
        [HttpGet]
        [Route("checktransaction/{{transactionId}}")]
        [ProducesResponseType(typeof(IEnumerable<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public ActionResult<ErrorCode> CheckTransaction(string transactionId)
        {
            if (ModelState.IsValid)
            {
                return _factory.GetManagerService(BalanceFactoryType.Game).CheckTransaction(transactionId);
            }

            return BadRequest();
        }
    }
}
