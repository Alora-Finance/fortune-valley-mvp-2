using FortuneValley.Core;
using System;
namespace FortuneValley.Domain.Entities
{
    [Serializable]
    public class ActiveInvestment
    {
        public string Id { get; private set; }
        public InvestmentDefinition Definition { get; private set; }
        public int NumberOfShares { get; private set; }
        public float AveragePurchasePrice { get; private set; }
        public float Principal { get; private set; }
        public float CurrentValue { get; private set; }
        public float TotalGain => CurrentValue - Principal;
        public float PercentageReturn => Principal > 0 ? ((CurrentValue / Principal) - 1f) * 100f : 0f;
        public int TicksHeld { get; private set; }
        public int PurchaseTick { get; private set; }

        public ActiveInvestment(InvestmentDefinition definition, int shareCount, float pricePerShare, int purchaseTick)
        {
            Id = Guid.NewGuid().ToString();
            Definition = definition;
            NumberOfShares = shareCount;
            AveragePurchasePrice = pricePerShare;
            Principal = shareCount * pricePerShare;
            CurrentValue = Principal;
            PurchaseTick = purchaseTick;
            TicksHeld = 0;
        }

        public void AddShares(int shareCount, float pricePerShare)
        {
            float newPrincipal = shareCount * pricePerShare;
            float totalPrincipal = Principal + newPrincipal;
            AveragePurchasePrice = totalPrincipal / (NumberOfShares + shareCount);
            NumberOfShares += shareCount;
            Principal = totalPrincipal;
            CurrentValue += newPrincipal;
        }

        public int RemoveShares(int shareCount)
        {
            int removed = Math.Min(shareCount, NumberOfShares);
            float proportion = (float)removed / NumberOfShares;
            Principal -= Principal * proportion;
            CurrentValue -= CurrentValue * proportion;
            NumberOfShares -= removed;
            return removed;
        }

        public void IncrementTicksHeld()
        {
            TicksHeld++;
        }

        public bool TryCompound(int currentTick)
        {
            float growth = /*Definition.CalculateGrowth(CurrentValue, TicksHeld);*/0;
            if (growth > 0)
            {
                CurrentValue += growth;
                return true;
            }
            return false;
        }
    }
}