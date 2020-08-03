using Balances;
using Microsoft.AspNetCore.Mvc;
using Singluar.Infrastructure.Factories;
using Singluar.Utilities.Enums;
using System.Collections.Generic;
using System.Net;

namespace Singluar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CasinoBalanceController : ControllerBase
    {
        private readonly BalanceFactory _factory;

        public CasinoBalanceController(BalanceFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Gets current casino balance
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("balance")]
        [ProducesResponseType(typeof(IEnumerable<decimal>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public ActionResult<decimal> Get()
        {
            return _factory.GetManagerService(BalanceFactoryType.Casino).GetBalance();
        }

        /// <summary>
        /// Checks transaction status
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("checktransaction/{{transactionId}}")]
        [ProducesResponseType(typeof(IEnumerable<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public ActionResult<ErrorCode> CheckTransaction(string transactionId)
        {
            if (ModelState.IsValid)
            {
                return _factory.GetManagerService(BalanceFactoryType.Casino).CheckTransaction(transactionId);
            }

            return BadRequest();
        }
    }
}
