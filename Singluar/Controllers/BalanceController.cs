using Balances;
using Microsoft.AspNetCore.Mvc;
using Singluar.Infrastructure.Factories;
using Singluar.Utilities.Attributes;
using Singluar.Utilities.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Singluar.Controllers
{
    [Route("api/")]
    [ApiController]
    public class BalanceController : ControllerBase
    {
        private readonly BalanceFactory _factory;
        private readonly IBalanceManager _gameManager;
        private readonly IBalanceManager _casinoManager;

        public BalanceController(BalanceFactory factory)
        {
            _factory = factory;
            _gameManager = _factory.GetManagerService(BalanceFactoryType.Game);
            _casinoManager = _factory.GetManagerService(BalanceFactoryType.Casino);
        }

        /// <summary>
        /// Gets current balance on casino side
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        [Route("balance")]
        [ProducesResponseType(typeof(IEnumerable<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public ActionResult<string> Get()
        {
            var currCasinoBalance = _casinoManager.GetBalance();

            return string.Format(Resources.Resources.YourCurrentCasinoBalance, currCasinoBalance);
        }

        /// <summary>
        /// Gets current balance on the both sides
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("balance/total")]
        [ProducesResponseType(typeof(IEnumerable<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public ActionResult<string> GetTotal()
        {
            var currGameBalance = _gameManager.GetBalance();
            var currCasinoBalance = _casinoManager.GetBalance();

            return string.Format(Resources.Resources.YourCurrentBalances, currGameBalance, currCasinoBalance);
        }

        /// <summary>
        /// Transfers specific amount from casino to game balance
        /// </summary>
        /// <param name="transactionid">Transaction ID</param>
        /// <param name="amount">Money amount</param>
        /// <returns></returns>
        [HttpPut("withdraw/{{transactionid}}/{{amount}}")]
        [ProducesResponseType(typeof((ErrorCode code, string message)), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<(ErrorCode code, string message)>> Withdraw(string transactionid, [IsPositive] decimal amount)
        {
            if (ModelState.IsValid)
            {
                var casResult = _casinoManager.DecreaseBalance(amount, transactionid);

                if (casResult != ErrorCode.Success & _casinoManager.CheckTransaction(transactionid) != ErrorCode.Success)
                {
                    return Ok(new
                    {
                        Code = casResult,
                        Message = Resources.DecreaseBalanceResources.ResourceManager.GetString(casResult.ToString()) ?? string.Empty
                    });
                }

                var balResult = _gameManager.IncreaseBalance(amount, transactionid);

                if (balResult != ErrorCode.Success & _gameManager.CheckTransaction(transactionid) != ErrorCode.Success)
                {
                    var rollbackRes = await RollbackTransaction(transactionid, BalanceFactoryType.Casino);

                    if (!rollbackRes.success)
                        return Ok(new
                        {
                            Code = rollbackRes.error,
                            Message = Resources.RollbackResources.ResourceManager.GetString(rollbackRes.error.ToString()) ?? string.Empty
                        });

                    return Ok(new
                    {
                        Code = rollbackRes.error,
                        Message = Resources.Resources.CasinoRolledBackWithDrawFailed
                    });
                }

                return Ok(new
                {
                    Code = ErrorCode.Success,
                    Message = Resources.Resources.WithdrawedSuccessfully
                });
            }

            return BadRequest();
        }

        /// <summary>
        /// Transfers specific amount from game to casino balance
        /// </summary>
        /// <param name="transactionid">Transaction ID</param>
        /// <param name="amount">Money amount</param>
        /// <returns></returns>
        [HttpPut]
        [Route("deposit/{{transactionid}}/{{amount}}")]
        [ProducesResponseType(typeof((ErrorCode code, string message)), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<(ErrorCode code, string message)>> Deposit(string transactionid, [IsPositive] decimal amount)
        {
            if (ModelState.IsValid)
            {
                var balResult = _gameManager.DecreaseBalance(amount, transactionid);

                if (balResult != ErrorCode.Success & _gameManager.CheckTransaction(transactionid) != ErrorCode.Success)
                {
                    return Ok(new
                    {
                        Code = balResult,
                        Message = Resources.DecreaseBalanceResources.ResourceManager.GetString(balResult.ToString()) ?? string.Empty
                    });
                }

                var casResult = _casinoManager.IncreaseBalance(amount, transactionid);

                if (casResult != ErrorCode.Success & _casinoManager.CheckTransaction(transactionid) != ErrorCode.Success)
                {
                    var rollbackRes = await RollbackTransaction(transactionid, BalanceFactoryType.Game);

                    if (!rollbackRes.success)
                        return Ok(new
                        {
                            Code = rollbackRes.error,
                            Message = Resources.RollbackResources.ResourceManager.GetString(rollbackRes.error.ToString()) ?? string.Empty
                        });

                    return Ok(new
                    {
                        Code = rollbackRes.error,
                        Message = Resources.Resources.GameRolledBackDepositFailed
                    });
                }

                return Ok(new
                {
                    Code = ErrorCode.Success,
                    Message = Resources.Resources.DepositSuccessful
                });
            }

            return BadRequest();
        }

        /// <summary>
        /// Rollbacks the specific transaction
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("rollback/{{transactionid}}")]
        [ProducesResponseType(typeof((bool success, string message)), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<(bool success, string message)>> Rollback(string transactionId)
        {
            if (ModelState.IsValid)
            {
                var transactionRes = CheckTransactions(transactionId);

                if (!transactionRes.success)
                    return Ok(new
                    {
                        Success = false,
                        Message = Resources.CheckTransactionResources.ResourceManager.GetString(transactionRes.errors.FirstOrDefault().ToString()) ?? string.Empty
                    });

                var rollbackRes = await RollbackTransaction(transactionId);

                return Ok(new
                {
                    Success = rollbackRes.success,
                    Message = Resources.RollbackResources.ResourceManager.GetString(rollbackRes.error.ToString()) ?? string.Empty
                });
            }

            return BadRequest();
        }

        #region private methods

        /// <summary>
        /// Checks transaction statuses for both casino and game balance sides
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <returns></returns>
        private (bool success, List<ErrorCode> errors) CheckTransactions(string transactionId)
        {
            var errors = new List<ErrorCode>();

            var balTransactionResult = _gameManager.CheckTransaction(transactionId);
            var casTransactionResult = _casinoManager.CheckTransaction(transactionId);

            if (balTransactionResult == ErrorCode.Success && casTransactionResult == ErrorCode.Success)
                return (success: true, errors: errors);

            if (balTransactionResult != ErrorCode.Success)
            {
                errors.Add(balTransactionResult);
            }

            if (casTransactionResult != ErrorCode.Success)
            {
                errors.Add(casTransactionResult);
            }

            return (success: false, errors: errors);
        }

        /// <summary>
        /// Checks transaction status for the specific balance type
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <param name="type">Balance type</param>
        /// <returns></returns>
        private ErrorCode CheckTransaction(string transactionId, BalanceFactoryType type)
        {
            switch (type)
            {
                case BalanceFactoryType.Game:
                    return _gameManager.CheckTransaction(transactionId);
                case BalanceFactoryType.Casino:
                    return _casinoManager.CheckTransaction(transactionId);
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Rollbacks transaction for specified balance type
        /// </summary>
        /// <param name="transactionId">Transactino ID</param>
        /// <param name="type">balance type (rollbacks both sides if null)</param>
        /// <returns></returns>
        private async Task<(bool success, ErrorCode error)> RollbackTransaction(string transactionId, BalanceFactoryType? type = null)
        {
            Task casTask = default;
            Task balTask = default;

            Action<ErrorCode> RepeatRollback = (ErrorCode err) =>
            {
                while (err != ErrorCode.TransactionAlreadyMarkedAsRollback)
                {
                    err = _casinoManager.Rollback(transactionId);
                }
            };

            if (type == null)
            {
                var casResult = _casinoManager.Rollback(transactionId);
                var balResult = _gameManager.Rollback(transactionId);

                if (casResult != ErrorCode.Success)
                {
                    casTask = Task.Run(() => RepeatRollback(casResult));
                }

                if (balResult != ErrorCode.Success)
                {
                    balTask = Task.Run(() => RepeatRollback(balResult));
                }
            }
            else
            {
                ErrorCode res;

                switch (type)
                {
                    case BalanceFactoryType.Game:
                        res = _gameManager.Rollback(transactionId);
                        if (res != ErrorCode.Success)
                        {
                            balTask = Task.Run(() => RepeatRollback(res));
                        }
                        break;
                    case BalanceFactoryType.Casino:
                        res = _casinoManager.Rollback(transactionId);
                        if (res != ErrorCode.Success)
                        {
                            //casTask = Task.Run(() =>
                            //{
                            //    while (res != ErrorCode.TransactionAlreadyMarkedAsRollback)
                            //    {
                            //        res = _casinoManager.Rollback(transactionId);
                            //    }
                            //});

                            casTask = Task.Run(() => RepeatRollback(res));
                        }
                        break;
                    default:
                        throw new ArgumentException();
                }
            }

            if (casTask?.Status == TaskStatus.Running)
                await casTask.ConfigureAwait(false);

            if (balTask?.Status == TaskStatus.Running)
                await balTask.ConfigureAwait(false);

            return (success: true, error: ErrorCode.Success);
        }



        #endregion
    }
}
