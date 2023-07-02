using Amazon;
using System.Runtime.CompilerServices;
using BKProtocol;
using BKProtocol.Enum;

namespace BKWebAPIComponent.Service.Match.Detail
{
    public static class RegionTypeExtension
    {
        public static RegionEndpoint ToRegionEndPoint(this RegionCode regionType)
        {
            switch (regionType)
            {
                case RegionCode.Asia:
                    return RegionEndpoint.APNortheast2;

                default:
                    return RegionEndpoint.APNortheast2;
            }
        }
    }
}
