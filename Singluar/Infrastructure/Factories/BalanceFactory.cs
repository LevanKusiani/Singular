using Balances;
using Singluar.Utilities.Enums;
using System;

namespace Singluar.Infrastructure.Factories
{
    public class BalanceFactory
    {
        public IBalanceManager GetManagerService(BalanceFactoryType type)
        {
            switch (type)
            {
                case BalanceFactoryType.Game:
                    return new GameBalanceManager();
                case BalanceFactoryType.Casino:
                    return new CasinoBalanceManager();
                default:
                    throw new ArgumentException();
            }
        }
    }
}
