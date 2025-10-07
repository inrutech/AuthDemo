using System;
using System.Diagnostics.Contracts;
using System.Numerics;
using AuthDemo.Domain.Client.EVM.Struct;
using Microsoft.Extensions.DependencyInjection;
using Zero.Core.Result;

namespace AuthDemo.Service.Admin;

public class TestService(IServiceProvider provider) : BaseService(provider)
{
    public async Task<SysResult<bool>> TestAsync()
    {
        List<TransferRequest> requests = [
            new TransferRequest
            {
                From = "TWS1onJnNTg8tJHomceqxBxTsUB1DHh7PV",  // USDT持有人地址
                To = "TNDTGoJ3dDvEmNHPCit9UUJVqFswaY7yvC",    // 收款方地址
                Token = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t", // Shasta USDT
                Amount = new BigInteger(1000 * 1_000_000), // USDT有6位小数
                BusinessId = 10001
            }
        ];


        return Result(true);
    }
}
