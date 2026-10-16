using System;

namespace LastSupply.Core
{
    // 식량,의약품,부품을 함께 담는 값
    public readonly struct ResourceAmount
    {
        public int Food { get; }
        public int Medicine { get; }
        public int Parts { get; }

        public bool IsEmpty => Food == 0 && Medicine == 0 && Parts == 0;

        public ResourceAmount(int food , int medicine , int parts)
        {
            if ( food < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(food));
            }

            if ( medicine < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(medicine));
            }

            if ( parts < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(parts));
            }

            Food = food;
            Medicine = medicine;
            Parts = parts;
        }

        public ResourceAmount Add(ResourceAmount amount)
        {
            return new ResourceAmount(checked(Food + amount.Food), checked(Medicine + amount.Medicine), checked(Parts + amount.Parts));
        }

        public ResourceAmount Add(RESOURCE_TYPE resourceType , int amount)
        {
            if ( amount < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            switch ( resourceType )
            {
                case RESOURCE_TYPE.FOOD:
                    return new ResourceAmount(checked(Food + amount) , Medicine , Parts);

                case RESOURCE_TYPE.MEDICINE:
                    return new ResourceAmount(Food , checked(Medicine + amount) , Parts);

                case RESOURCE_TYPE.PARTS:
                    return new ResourceAmount(Food , Medicine , checked(Parts + amount));

                default:
                    throw new ArgumentOutOfRangeException(nameof(resourceType));
            }
        }
    }
}
