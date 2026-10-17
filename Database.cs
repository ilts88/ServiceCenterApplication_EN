using Microsoft.EntityFrameworkCore;

namespace Database
{
     public class MySqlDatabase
    {
        protected string configuration;
        public MySqlDatabase(string server,string user,string password,string database)
        {
            configuration = "server=" + server + ";user=" + user + ";password=" + password + ";database=" + database + ";";   //creating a configuration string used to create an object of the MySqlDatabaseContext class
        }
        public int GetRequestsMaximalId()          //getting the maximal repair request id
        {
            int id = 0;                                                                //if there are no repair requests in the database, the method returns 0
            using (MySqlDatabaseContext db = new MySqlDatabaseContext(configuration))
            {
                var result = db.Database.SqlQueryRaw<int?>("SELECT MAX(id) FROM requests");
                if (result.ToList()[0] != null)
                    id = result.ToList()[0].Value;
            }
            return id;
        }

        public void CreateRequest(Request request)   //creating a repair request
        {
            using (MySqlDatabaseContext db=new MySqlDatabaseContext(configuration))
            {
                
                db.Database.ExecuteSqlRaw("INSERT requests(id,fullname,phone,email,model,serial_number,purchase_date,address,malfunction,created_at,status,service_comment) VALUE ({0},{1},{2},{3},{4},{5},{6},{7},{8},NOW(),{9},{10})",request.id,request.fullname,request.phone,request.email,request.model,request.serial_number,request.purchase_date,request.address,request.malfunction,request.status,request.service_comment);
            }
        }

        public Request? GetRequestById(int id)      //getting a repair request by id
        {
            Request? request = null;
            using (MySqlDatabaseContext db=new MySqlDatabaseContext(configuration))
            {
                var result=db.requests.FromSqlRaw("SELECT * FROM requests WHERE id={0}",id);
                
                
                if (result.ToList().Count>0)
                    if (result.ToList()[0] != null)
                        request = result.ToList()[0];
                
                
            }
            return request;
        }
        public List<Request> GetAllRequests(string order_by)         //getting all repair requests (requests are sorted according to the sorting parameter)
        {
            List<Request> requests = null!;
            using (MySqlDatabaseContext db=new MySqlDatabaseContext(configuration))
            {
                requests=db.requests.FromSqlRaw("SELECT * FROM requests ORDER BY id " + order_by).ToList();
            }

            return requests;
        }
        public List<Request>GetRequestsByStatus(string order_by,string status)      //getting all repair requests with the specified status (requests are sorted according to the sorting parameter)
        {
            List<Request> requests = null!;
            using(MySqlDatabaseContext db=new MySqlDatabaseContext(configuration))
            {
                requests = db.requests.FromSqlRaw("SELECT * FROM requests WHERE status='" + status + "' ORDER BY id " + order_by).ToList();
            }

            return requests;
        }

        public void UpdateRequest(int id,string status,string service_comment)     //updating the status and service center comment for the repair request with the specified id
        {
            using (MySqlDatabaseContext db=new MySqlDatabaseContext(configuration))
            {
                db.Database.ExecuteSqlRaw("UPDATE requests SET status={0},service_comment={1} WHERE id={2}",status,service_comment,id);
            }
        }
        public int DeleteOldRequestById(int id)           //deleting a repair request with the specified id
        {
            int result_id = 0;
            using (MySqlDatabaseContext db=new MySqlDatabaseContext(configuration))
            {
                string? status = null;                    //before deleting, the status and the time passed since the creation of the request with the specified id are retrieved from the database and evaluated to determine whether the deletion criteria are met
                int? time_passed = null;
                var status_response = db.Database.SqlQueryRaw<string?>("SELECT status FROM requests WHERE id={0}",id);
                if (status_response.ToList().Count > 0)
                    status = status_response.ToList()[0];
                var time_passed_response = db.Database.SqlQueryRaw<int?>("SELECT DATEDIFF(NOW(),created_at) FROM requests WHERE id={0}",id);     //to calculate the time since the request was created, the DATEDIFF() function in MySQL is used
                if (time_passed_response.ToList().Count > 0)
                    time_passed = time_passed_response.ToList()[0];
                if (status != null &&  time_passed != null && (status=="resolved" || status=="rejected") && time_passed > 180)    //if the status and time since creation meet the deletion criteria, the request is deleted
                {
                    db.Database.ExecuteSqlRaw("DELETE FROM requests WHERE id={0}",id);
                    result_id = id;
                }                                                                              //if the deletion was performed, the method returns the id of the deleted request, if the deletion was not performed, 0 is returned
            }
            return result_id;
        }

        public List<int> DeleteAllOldRequests()                   //deleting all repair requests that match the deletion criteria
        {
            List<int> deletedIds = new List<int>();
            using (MySqlDatabaseContext db=new MySqlDatabaseContext(configuration))
            {
                List<int?> IdForDeleting = db.Database.SqlQueryRaw<int?>("SELECT id FROM requests WHERE (DATEDIFF(NOW(),created_at)>180) AND (status='resolved' OR status='rejected')").ToList();         //getting from the database a list of ids of all requests that meet the deletion criteria
                foreach (int? id in IdForDeleting)
                {
                    if (id != null)
                    {
                        int deleted_id = DeleteOldRequestById(id.Value);             //call the method to delete a request by id for each id from the received list
                        deletedIds.Add(deleted_id);
                    }
                    
                }
            }
            return deletedIds;                       //the method returns a list of ids of all deleted requests
        }

        public int GetNewsMaximalId()            //getting the maximal article id
        {
            int id = 0;                                                                  //if there are no articles in the database, the method returns 0
            using (MySqlDatabaseContext db = new MySqlDatabaseContext(configuration))
            {
                var result = db.Database.SqlQueryRaw<int?>("SELECT MAX(id) FROM news");
                if (result.ToList()[0] != null)
                    id = result.ToList()[0].Value;
            }
            return id;
        }

        public void CreateArticle(Article article)        //creating an article
        {
            using (MySqlDatabaseContext db = new MySqlDatabaseContext(configuration))
            {
                db.Database.ExecuteSqlRaw("INSERT news(id,title,content,created_at) VALUE ({0},{1},{2},NOW())",article.id,article.title,article.content);
            }
        }

        public List<Article> GetAllNews(string order_by)   //getting all articles (articles are sorted according to the sorting parameter)
        {
            List<Article> news = null!;
            using (MySqlDatabaseContext db = new MySqlDatabaseContext(configuration))
            {
                news = db.news.FromSqlRaw("SELECT * FROM news ORDER BY id " + order_by).ToList();
            }

            return news;
        }

        public Article? GetArticleById(int id)        //getting an article by id
        {
            Article? article = null;
            using (MySqlDatabaseContext db = new MySqlDatabaseContext(configuration))
            {
                var result = db.news.FromSqlRaw("SELECT * FROM news WHERE id={0}",id);
                

                if (result.ToList().Count > 0)
                    if (result.ToList()[0] != null)
                        article = result.ToList()[0];


            }
            return article;
        }

        public void UpdateArticle(int id, string title, string content)            //updating an article with the specified id
        {
            using (MySqlDatabaseContext db = new MySqlDatabaseContext(configuration))
            {
                db.Database.ExecuteSqlRaw("UPDATE news SET title={0},content={1} WHERE id={2}",title,content,id);
            }
        }

        public void DeleteArticleById(int id)            //deleting an article with the specified id
        {
            
            using (MySqlDatabaseContext db = new MySqlDatabaseContext(configuration))
            {
                
                    db.Database.ExecuteSqlRaw("DELETE FROM news WHERE id={0}",id);
                   
            }
            
        }
    }
    public class MySqlDatabaseContext : DbContext
    {
        protected string mysqlconfiguration;
        public DbSet<Request> requests { get; set; } = null!;         //creating a repair request table object in the database
        public DbSet<Article> news { get; set; } = null!;             //creating a publication table object in the database

        public MySqlDatabaseContext(string configuration)
        {
            mysqlconfiguration = configuration;                    //getting database configuration data
            Database.EnsureCreated();                              //creating a database if it doesn't exist
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)           //establishing a connection to the database
        {
            optionsBuilder.UseMySql(mysqlconfiguration, new MySqlServerVersion(new Version(8, 0, 31)));
        }

    }

    public record Request(int id,string fullname,string phone,string email,string model,string serial_number,DateOnly purchase_date,string address,string malfunction,DateTime created_at,string status,string? service_comment);     //creating a data type that characterizes a repair request
    public record Article(int id,string title, string content,DateTime created_at);               //creating a data type that characterizes an article
}