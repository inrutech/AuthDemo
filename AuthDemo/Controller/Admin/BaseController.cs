using System;
using Microsoft.AspNetCore.Mvc;

namespace AuthDemo.Controller.Admin;

/// <summary>
/// 管理员基础控制器
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[ApiExplorerSettings(GroupName = "admin")]
public class BaseController { }