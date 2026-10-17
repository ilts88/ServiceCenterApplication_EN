using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

public class AuthenticationPageMiddleware
{
    RequestDelegate next;

    public AuthenticationPageMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context,IConfiguration appConfiguration)
    {
        string GeneratedKey = StringGenerator.GetGeneratedKey(8,16);           //getting the generated authentication key
        string? AdminLogin = appConfiguration["adminlogin"];
        string? AdminPassword = appConfiguration["adminpassword"];             //getting the administrator login and password from the configuration file
        string? login = null;
        string? password = null;
        if (context.Request.HasFormContentType)
        {
            if (context.Request.Form.ContainsKey("login"))
                login = context.Request.Form["login"];
            if (context.Request.Form.ContainsKey("password"))                   //attempt to get the entered login and password from the submitted request
                password = context.Request.Form["password"];
        }
        var Identity = context.User.Identity;                                   //attempt to get user authentication data, including the currently used authentication key
        var Key = context.User.FindFirst("key");
        if (AdminLogin != null && AdminPassword != null)                        //checking the success of getting the administrator login and password from the configuration files
        {
            if (Identity != null && Key != null && Identity.IsAuthenticated && Key.Value == GeneratedKey)
            {
                await next.Invoke(context);                             //if the user is authenticated and uses a valid authentication key, access to the administrator panel is granted, and the next middleware is invoked
            }
            else if (login != null && password != null)        //otherwise, if the user has entered a login and password, they are checked
            {
                if (login == AdminLogin && password == AdminPassword)      //if the user enters the correct login and password, he is authenticated and assigned a valid authentication key
                {
                    var claims = new List<Claim> { new Claim("key", GeneratedKey) };
                    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                    await context.SignInAsync(claimsPrincipal);
                    await next.Invoke(context);                             //the user is also immediately given access to the administrator panel and the next middleware is invoked
                }
                else
                {
                    context.Response.StatusCode = 403;                 //if the user enters an incorrect login or password, the server sends a response with an authentication error status code
                }
            }
            else                                       //if the user is not authenticated using the valid authentication key, and has not entered a login and password, then he is sent a page for entering a login and password
            {
                if (Identity != null && Identity.IsAuthenticated)      //in this case, if the user was previously authenticated, his authentication data is deleted
                    await context.SignOutAsync();
                await context.Response.SendFileAsync("authenticationpage.html");
            }
        }
        else                                          //if the server is unable to get the administrator login and password from the configuration file, the server sends a response with an error status code and a corresponding message
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync("The server is unavailable, a critical configuration error has occurred");
        }
    }
}