using System;
using AuthDemo.Service.Admin;
using Microsoft.AspNetCore.Mvc;
using Zero.Core.Result;

namespace AuthDemo.Controller.Admin;

public class TestController(TestService service) : BaseController
{
    [HttpGet("Test")]
    public Task<SysResult<bool>> TestAsync()
    {
        return service.TestAsync();
    }
}
