using Database;
using StringUtil;

public class AdminPageMiddleware
{
    RequestDelegate next;

    public AdminPageMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context,IConfiguration appConfiguration)
    {
        string? server = appConfiguration["server"];
        string? user = appConfiguration["user"];
        string? password = appConfiguration["password"];                    //getting data from a configuration file for connecting to a database
        string? database = appConfiguration["database"];

        if (server != null && user != null && password != null && database != null)
        {
            MySqlDatabase ServiceCenterDatabase = new MySqlDatabase(server, user, password, database);             //creating a database class object for further interaction
            if (context.Request.Path == "/adminpage/newspage")
            {
                await context.Response.SendFileAsync("adminnewspage.html");                                       //sending news management page
            }
            else if (context.Request.Path == "/adminpage/getrequests" && context.Request.Method == "GET")
            {
                string? order_by = null;
                string? select = null;
                if (context.Request.Query.ContainsKey("order_by"))                                              //if a request to get all repair requests was sent to the server, an attempt is made to retrieve the user-selected sorting and display parameters for requests from the query string parameters
                    order_by = context.Request.Query["order_by"];
                if (context.Request.Query.ContainsKey("select"))
                    select = context.Request.Query["select"];
                if ((order_by == "ASC" || order_by == "DESC") && (select == "all" || select == "processing" || select == "appointed" || select == "rejected" || select == "resolved"))
                {
                    List<Request> requests;                       //if the received parameters for sorting and displaying repair requests were correct, the server sends to the user all requests that match the selected parameters
                    if (select == "all")
                    {
                        requests = ServiceCenterDatabase.GetAllRequests(order_by);
                    }
                    else
                    {
                        requests = ServiceCenterDatabase.GetRequestsByStatus(order_by, select);
                    }
                    await context.Response.WriteAsJsonAsync(requests);
                }
                else                                     //if the received parameters do not pass the check, the server sends a response with an error status code and a corresponding message
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync("The request contains invalid data");
                }
            }
            else if (context.Request.Path == "/adminpage/getrequestbyid" && context.Request.Method == "GET")
            {
                string? id = null;
                int int_id = 0;
                bool IsNumber = false;

                if (context.Request.Query.ContainsKey("id"))               //if a request to get a repair request by id was sent to the server, an attempt is made to extract the repair request id from the query string parameters
                    id = context.Request.Query["id"];
                if (id != null)
                    IsNumber = int.TryParse(id, out int_id);
                if (IsNumber)                                              //if the repair request id was successfully received and converted to the int type, a search for a request with the received id is performed in the database
                {
                    Request? request = ServiceCenterDatabase.GetRequestById(int_id);
                    if (request != null)
                    {
                        await context.Response.WriteAsJsonAsync(request);              //if the request was found, the server sends it to the user
                    }
                    else                                                        //if the request was not found, and also if the passed id was not successfully converted to the int type, the server sends a response with the corresponding status code and an error message
                    {
                        context.Response.StatusCode = 400;                             
                        await context.Response.WriteAsJsonAsync("Request not found");
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync("The provided ID is not a number");
                }

            }
            else if (context.Request.Path == "/adminpage/updaterequest" && context.Request.Method == "PUT")
            {
                Request? request;
                try
                {
                    request = await context.Request.ReadFromJsonAsync<Request>();         //if a request to update the repair request data was sent to the server, an attempt is made to read the repair request from a JSON object
                }
                catch
                {
                    request = null;
                }
                if (request != null && request.service_comment != null)                      //the success of reading the request data is checked
                {
                    bool IsValidStatus = StringValidator.IsValidStatus(request.status);                                     //the correctness of the updated data, the status of the request and the service center comment are checked
                    bool IsValidService_comment = StringValidator.IsValidService_comment(request.service_comment);
                    if (IsValidStatus && IsValidService_comment)
                    {
                        ServiceCenterDatabase.UpdateRequest(request.id, request.status, request.service_comment);           //if the updated data has passed check, the changes made are entered into the database
                        await context.Response.WriteAsJsonAsync(request);                                                   //the server sends an updated repair request to the user
                    }
                    else                                                                                                //if the updated data does not pass the check, and if the repair request was not received, the server sends a response with the corresponding status code and error message
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync("Invalid format of the entered data");
                    }

                }
                else
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync("Data was not received");
                }
            }
            else if (context.Request.Path == "/adminpage/deleterequest" && context.Request.Method == "DELETE")
            {
                string? id = null;
                int int_id = 0;
                bool IsNumber = false;

                if (context.Request.Query.ContainsKey("id"))                       //if the server has received a request to delete a repair request by id, an attempt is made to extract the repair request id from the query string parameters
                    id = context.Request.Query["id"];
                if (id != null)
                    IsNumber = int.TryParse(id, out int_id);
                if (IsNumber)                                                      //if the repair request id was successfully received and converted to an int type, an attempt is made to delete the specified request from the database
                {
                    int result_id = ServiceCenterDatabase.DeleteOldRequestById(int_id);
                    if (result_id != 0)                                                      //if the request with the specified id was successfully deleted, the server returns the id of the deleted request to the user
                        await context.Response.WriteAsJsonAsync(result_id);
                    else                                                                   //if the request is not deleted due to deletion criteria checks, and if the passed id cannot be correctly converted to an int type, the server sends a response with the corresponding status code and error message
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync("The request cannot be deleted.");
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync("The provided ID is not a number");
                }
            }
            else if (context.Request.Path == "/adminpage/deletealloldrequests" && context.Request.Method == "DELETE")
            {
                List<int> deletedIds = ServiceCenterDatabase.DeleteAllOldRequests();                       //if the server was sent a request to delete all repair requests that match the deletion criteria, the server deletes all matching requests and returns to the user an array containing the id of all deleted requests
                await context.Response.WriteAsJsonAsync(deletedIds);
            }
            else if (context.Request.Path == "/adminpage/createarticle" && context.Request.Method == "POST")
            {
                Article? result;
                try
                {
                    result = await context.Request.ReadFromJsonAsync<Article>();            //if the server was sent a request to create a new article, an attempt is made to read the article from a JSON object
                }
                catch
                {
                    result = null;
                }
                if (result != null)                    //the success of reading the article data is checked
                {
                    bool IsValidTitle = StringValidator.IsValidArticleTitle(result.title);            //a check is performed to ensure that the publication title and content conform to the expected format
                    bool IsValidContent = StringValidator.IsValidArticleContent(result.content);
                    if (IsValidTitle && IsValidContent)                                              //if the article has passed the checks, an attempt is made to create the article
                    {
                        int id = ServiceCenterDatabase.GetNewsMaximalId();                       //getting the current maximal article id
                        if (id < int.MaxValue)                                                   //checking the availability of numbers in the system to create new id
                        {
                            id++;                                                                //if there are numbers available in the system to create an id, a new id is created by adding one to the current maximal id
                            Article newarticle = new Article(id, result.title, result.content, result.created_at);
                            ServiceCenterDatabase.CreateArticle(newarticle);
                            Article? response_article = ServiceCenterDatabase.GetArticleById(id);
                            await context.Response.WriteAsJsonAsync(response_article);                 //a new article is created in the database, the server returns the data of the created article to the user
                        }
                        else                                     //if there are not enough numbers available in the system to create new id, or if the data was not received or did not pass the format check, the server sends a response with the corresponding status code and error message
                        {
                            context.Response.StatusCode = 503;
                            await context.Response.WriteAsJsonAsync("The database is full");
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync("Invalid format of the entered data");
                    }

                }
                else
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync("Data was not received");
                }
            }
            else if (context.Request.Path == "/adminpage/getallnews" && context.Request.Method == "GET")
            {
                string? order_by = null;
                if (context.Request.Query.ContainsKey("order_by"))      //if a request to get all publications was sent to the server, an attempt is made to retrieve the user-selected sorting parameters for publications from the query string parameters
                    order_by = context.Request.Query["order_by"];
                if (order_by == "ASC" || order_by == "DESC")
                {
                    List<Article> news = ServiceCenterDatabase.GetAllNews(order_by);       //if the received publication sorting parameters were correct, the server sends all articles to the user, applying the selected sorting parameters
                    await context.Response.WriteAsJsonAsync(news);
                }
                else                                                                  //if the received parameters do not pass the check, the server sends a response with an error status code and a corresponding message
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync("The request contains invalid data");
                }
            }
            else if (context.Request.Path == "/adminpage/getarticlebyid" && context.Request.Method == "GET")
            {
                string? id = null;
                int int_id = 0;
                bool IsNumber = false;

                if (context.Request.Query.ContainsKey("id"))                        //if a request to get an article by id was sent to the server, an attempt is made to extract the article id from the query string parameters
                    id = context.Request.Query["id"];
                if (id != null)
                    IsNumber = int.TryParse(id, out int_id);
                if (IsNumber)                                              //if the article id was successfully received and converted to the int type, a search for an article with the received id is performed in the database
                {
                    Article? article = ServiceCenterDatabase.GetArticleById(int_id);
                    if (article != null)
                    {
                        await context.Response.WriteAsJsonAsync(article);           //if the article was found, the server sends it to the user
                    }
                    else                                                            //if the article was not found, and also if the passed id was not successfully converted to the int type, the server sends a response with the corresponding status code and an error message
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync("Article not found");
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync("The provided ID is not a number");
                }

            }
            else if (context.Request.Path == "/adminpage/updatearticle" && context.Request.Method == "PUT")
            {
                Article? article;
                try
                {
                    article = await context.Request.ReadFromJsonAsync<Article>();            //if a request to update the article data was sent to the server, an attempt is made to read the article from a JSON object
                }
                catch
                {
                    article = null;
                }
                if (article != null)                                                            //the success of reading the article data is checked
                {
                    bool IsValidTitle = StringValidator.IsValidArticleTitle(article.title);            //a check is performed to ensure that the publication title and content conform to the expected format
                    bool IsValidContent = StringValidator.IsValidArticleContent(article.content);
                    if (IsValidTitle && IsValidContent)
                    {
                        ServiceCenterDatabase.UpdateArticle(article.id, article.title, article.content);             //if the updated data has passed check, the changes made are entered into the database
                        await context.Response.WriteAsJsonAsync(article);                                            //the server sends an updated article to the user
                    }
                    else                                                                                     //if the updated data does not pass the check, and if the article was not received, the server sends a response with the corresponding status code and error message
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync("Invalid format of the entered data");
                    }

                }
                else
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync("Data was not received");
                }
            }
            else if (context.Request.Path == "/adminpage/deletearticle" && context.Request.Method == "DELETE")
            {
                string? id = null;
                int int_id = 0;
                bool IsNumber = false;

                if (context.Request.Query.ContainsKey("id"))           //if the server has received a request to delete an article by id, an attempt is made to extract the article id from the query string parameters
                    id = context.Request.Query["id"];
                if (id != null)
                    IsNumber = int.TryParse(id, out int_id);
                if (IsNumber)                                          //if the article id was successfully received and converted to an int type, the specified article is deleted from the database
                {
                    ServiceCenterDatabase.DeleteArticleById(int_id);
                    await context.Response.WriteAsJsonAsync(int_id);
                }
                else                                                   //if the passed id cannot be correctly converted to an int type, the server sends a response with an error status code and a corresponding message
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync("The provided ID is not a number");
                }
            }
            else if (context.Request.Path == "/adminpage/admin_frame.css")
            {
                await context.Response.SendFileAsync("admin_frame.css");            //sending the admin page layout file
            }
            else
            {
                await context.Response.SendFileAsync("adminpage.html");             //sending the admin page
            }
        }
        else                                                           //if the database connection data was not received from the configuration file, the server sends a response with an error status code and a corresponding message
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync("The server is unavailable, a critical configuration error has occurred");
        }
    }
}