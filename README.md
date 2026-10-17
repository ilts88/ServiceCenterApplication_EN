# ServiceCenterApplication_EN

This web application is a system for an electronics company’s service center. This is a solution for interaction between the customers who need repairs and service center specialists. With its help is possible to receive repair requests from the customers, process them and inform customers about the repair progress. 

Application is split into two parts. First part is available to all users. There they can create repair requests and check requests statuses. Second part is available only for site administrator. To access it, the user needs to go to the address /adminpage (interface of the system on purpose doesn’t have any mention of it) and enter login and password. In this part of the application management of the repair requests is available.

In addition to the repair requests processing functionality system also has company’s news publication section. It is also split into two parts. In the first part, that can be accessed by all users, browsing of all published news is available. In the second part, that can be accessed only by administrators, management of the news publication is available.

Web application is built on the ASP.NET Core 8.0 framework and configured to connect to a MySQL database. For working with database Entity Framework Core is used, connection established using Pomelo.EntityFrameworkCore.MySql provider. The database connection is configured using the DatabaseConfiguration.json configuration file. Also, admin login data can be configured using the AdminAccessConfiguration.json configuration file.
