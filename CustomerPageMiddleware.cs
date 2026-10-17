using Database;
using StringUtil;
public class CustomerPageMiddleware
{
    RequestDelegate next;

    public CustomerPageMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context,IConfiguration appConfiguration)
    {
        string? server = appConfiguration["server"];         //getting data from a configuration file for connecting to a database
        string? user = appConfiguration["user"];
        string? password = appConfiguration["password"];
        string? database = appConfiguration["database"];

        if (server != null && user != null && password != null && database != null)
        {
            MySqlDatabase ServiceCenterDatabase = new MySqlDatabase(server, user, password, database);      //creating a database class object for further interaction
            if (context.Request.Path.StartsWithSegments("/adminpage"))
            {
                await next.Invoke(context);                                //if the request is related to the part of the system intended for administrator, the next middleware is invoked
            }
            else if (context.Request.Path == "/createrequest" && context.Request.Method == "POST")
            {
                Request? result;
                try
                {
                    result = await context.Request.ReadFromJsonAsync<Request>();       //if a request to create a repair request was sent to the server, an attempt is made to read the repair request from a JSON object
                }
                catch
                {
                    result = null;
                }
                if (result != null)                                         //the success of reading the request data is checked
                {
                    int id = ServiceCenterDatabase.GetRequestsMaximalId();   //getting the current maximal repair request id
                    int year = 2000 + (id / 10000000);
                    int month = id % 10000000 / 100000;                      //in the system, the id of all repair requests contains the date of creation of the request at the beginning
                    if (month == 0)
                        month++;                                             //therefore, in order to evaluate which id to assign to a new request, the creation date of the maximal request id is evaluated
                    int day = id % 100000 / 1000;
                    if (day == 0)                                            //if there has been no previous request in the system, the largest id will be zero
                        day++;                                               //since the DateOnly class object constructor can only be passed month and day values ​​that start with one, in this case the day and month values ​​are increased by one for further correct comparison
                    DateOnly date_id = new DateOnly(year, month, day);
                    DateOnly current_date = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                    if (date_id < current_date)                                                                           //if the maximal id was created earlier than today, then the new request is assigned the first id created based on today's date
                    {
                        id = (DateTime.Now.Year - 2000) * 10000000 + (DateTime.Now.Month) * 100000 + DateTime.Now.Day * 1000;
                    }
                    else                                                                                                  //otherwise, the new request is assigned the maximal id, increased by one
                    {
                        id++;                                                                        
                    }
                    if (id % 1000 < 999)                       //the system has a daily limit on the number of new requests
                    {
                        Request newrequest = new Request(id, result.fullname, result.phone, result.email, result.model, result.serial_number, result.purchase_date, result.address, result.malfunction, result.created_at, result.status, result.service_comment);
                        bool IsValidNewRequest = StringValidator.IsValidNewRequest(newrequest);
                        if (IsValidNewRequest)                                                         //the content of the repair request is checked for correctness
                        {
                            ServiceCenterDatabase.CreateRequest(newrequest);                           //if all checks are successful, a new repair request is created using the database class object
                            await context.Response.WriteAsJsonAsync(newrequest);                       //the server returns the new request data to the user
                        }
                        else                                                                           //if the received data does not pass one of the checks, the server sends a response with the corresponding status code and an error message
                        {
                            
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsJsonAsync("Invalid format of the entered data");
                        }

                    }
                    else
                    {
                        context.Response.StatusCode = 503;
                        await context.Response.WriteAsJsonAsync("Daily request limit exceeded");
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync("Data was not received");
                }
            }
            else if (context.Request.Path == "/requestpage")                   
            {
                await context.Response.SendFileAsync("requestpage.html");              //sending the repair request creation page
            }
            else if (context.Request.Path == "/getrequeststatus" && context.Request.Method == "GET")
            {
                string? id = null;
                int int_id = 0;
                bool IsNumber = false;

                if (context.Request.Query.ContainsKey("id"))            //if a request to get the status of a repair request was sent to the server, an attempt is made to extract the repair request id from the query string parameters
                    id = context.Request.Query["id"];
                if (id != null)
                    IsNumber = int.TryParse(id, out int_id);
                if (IsNumber)                                           //if the repair request id was successfully received and converted to the int type, a search for a request with the received id is performed in the database
                {
                    Request? request = ServiceCenterDatabase.GetRequestById(int_id);
                    if (request != null)
                    {
                        var status_info = new { request.status, request.service_comment };               //if a request with the specified id was found, to prevent disclosure of clients' personal data, the server creates a new object based on the found request, containing information only about the request status and service center comment, and sends it to the user
                        await context.Response.WriteAsJsonAsync(status_info);
                    }
                    else
                    {
                        context.Response.StatusCode = 204;                 //if the request was not found, the user is sent a status code indicating that there was no result
                    }

                }
                else                                                    //if the received id could not be correctly converted to the int type, the server sends a response with an error status code and a corresponding message
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync("The provided ID is not a number");
                }



            }
            else if (context.Request.Path == "/getallnews" && context.Request.Method == "GET")
            {
                List<Article> news = ServiceCenterDatabase.GetAllNews("DESC");            //if the server was sent a request to get all news, it get all news from the database, sorted in descending order, and sends them to the user
                await context.Response.WriteAsJsonAsync(news);
            }
            else if (context.Request.Path == "/statuspage")
            {
                await context.Response.SendFileAsync("statuspage.html");                  //sending a repair request status check page
            }
            else if (context.Request.Path == "/newspage")
            {
                await context.Response.SendFileAsync("newspage.html");                    //sending a news page
            }
            else if (context.Request.Path == "/aboutpage")
            {
                await context.Response.SendFileAsync("aboutpage.html");                   //sending a page with information about the company
            }
            else if (context.Request.Path == "/LOGO.png")
            {
                await context.Response.SendFileAsync("LOGO.png");                         //sending images for the company warranty advertising
            }
            else if (context.Request.Path == "/1.png")
            {
                await context.Response.SendFileAsync("1.png");
            }
            else if (context.Request.Path == "/2.png")
            {
                await context.Response.SendFileAsync("2.png");
            }
            else if (context.Request.Path == "/3.png")
            {
                await context.Response.SendFileAsync("3.png");
            }
            else if (context.Request.Path == "/4.png")
            {
                await context.Response.SendFileAsync("4.png");
            }
            else if (context.Request.Path == "/5.png")
            {
                await context.Response.SendFileAsync("5.png");
            }
            else if (context.Request.Path == "/6.png")
            {
                await context.Response.SendFileAsync("6.png");
            }
            else if (context.Request.Path == "/white.png")
            {
                await context.Response.SendFileAsync("white.png");
            }
            else if (context.Request.Path == "/frame.css")
            {
                await context.Response.SendFileAsync("frame.css");                               //sending the page layout file
            }
            else if (context.Request.Path == "/warranty_promotion_animation.css")
            {
                await context.Response.SendFileAsync("warranty_promotion_animation.css");        //sending an animation file of the company's warranty advertisement
            }
            else
            {
                await context.Response.SendFileAsync("mainpage.html");                           //sending the main page
            }
        }
        else                                                                         //if the database connection data was not received from the configuration file, the server sends a response with an error status code and a corresponding message
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync("The server is unavailable, a critical configuration error has occurred");
        }
    }
}