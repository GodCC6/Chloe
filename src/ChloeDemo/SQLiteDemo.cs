﻿using Chloe;
using Chloe.SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChloeDemo
{
    class SQLiteDemo
    {
        /* WARNING: DbContext 是非线程安全的，正式使用不能设置为 static，并且用完务必要调用 Dispose 方法销毁对象 */
        static SQLiteContext context = new SQLiteContext(new SQLiteConnectionFactory("Data Source=..\\..\\..\\Chloe.db;"));
        public static void Run()
        {
            BasicQuery();
            JoinQuery();
            AggregateQuery();
            GroupQuery();
            ComplexQuery();
            QueryWithNavigation();
            Insert();
            Update();
            Delete();
            Method();
            ExecuteCommandText();
            DoWithTransaction();
            DoWithTransactionEx();

            ConsoleHelper.WriteLineAndReadKey();
        }


        public static void BasicQuery()
        {
            IQuery<User> q = context.Query<User>();

            q.Where(a => a.Id == 1).FirstOrDefault();
            /*
             * SELECT [Users].[Id] AS [Id],[Users].[Name] AS [Name],[Users].[Gender] AS [Gender],[Users].[Age] AS [Age],[Users].[CityId] AS [CityId],[Users].[OpTime] AS [OpTime] FROM [Users] AS [Users] WHERE [Users].[Id] = 1 LIMIT 1 OFFSET 0
             */


            //可以选取指定的字段
            q.Where(a => a.Id == 1).Select(a => new { a.Id, a.Name }).FirstOrDefault();
            /*
             * SELECT [Users].[Id] AS [Id],[Users].[Name] AS [Name] FROM [Users] AS [Users] WHERE [Users].[Id] = 1 LIMIT 1 OFFSET 0
             */


            //分页
            var result = q.Where(a => a.Id > 0).OrderBy(a => a.Age).Skip(20).Take(10).ToList();
            /*
             * SELECT [Users].[Id] AS [Id],[Users].[Name] AS [Name],[Users].[Gender] AS [Gender],[Users].[Age] AS [Age],[Users].[CityId] AS [CityId],[Users].[OpTime] AS [OpTime] FROM [Users] AS [Users] WHERE [Users].[Id] > 0 ORDER BY [Users].[Age] ASC LIMIT 10 OFFSET 20
             */


            /* like 查询 */
            q.Where(a => a.Name.Contains("so") || a.Name.StartsWith("s") || a.Name.EndsWith("o")).ToList();
            /*
             * SELECT 
             *      [Users].[Gender] AS [Gender],[Users].[Age] AS [Age],[Users].[CityId] AS [CityId],[Users].[OpTime] AS [OpTime],[Users].[Id] AS [Id],[Users].[Name] AS [Name] 
             * FROM [Users] AS [Users] 
             * WHERE ([Users].[Name] LIKE '%' || 'so' || '%' OR [Users].[Name] LIKE 's' || '%' OR [Users].[Name] LIKE '%' || 'o')
             */


            /* in 一个数组 */
            List<User> users = null;
            List<int> userIds = new List<int>() { 1, 2, 3 };
            users = q.Where(a => userIds.Contains(a.Id)).ToList(); /* list.Contains() 方法组合就会生成 in一个数组 sql 语句 */
            /*
             * SELECT 
             *      [Users].[Gender] AS [Gender],[Users].[Age] AS [Age],[Users].[CityId] AS [CityId],[Users].[OpTime] AS [OpTime],[Users].[Id] AS [Id],[Users].[Name] AS [Name]
             * FROM [Users] AS [Users] 
             * WHERE [Users].[Id] IN (1,2,3)
             */


            /* in 子查询 */
            users = q.Where(a => context.Query<City>().Select(c => c.Id).ToList().Contains((int)a.CityId)).ToList(); /* IQuery<T>.ToList().Contains() 方法组合就会生成 in 子查询 sql 语句 */
            /*
             * SELECT 
             *      [Users].[Gender] AS [Gender],[Users].[Age] AS [Age],[Users].[CityId] AS [CityId],[Users].[OpTime] AS [OpTime],[Users].[Id] AS [Id],[Users].[Name] AS [Name]
             * FROM [Users] AS [Users] 
             * WHERE [Users].[CityId] IN (SELECT [City].[Id] AS [C] FROM [City] AS [City])
             */


            /* distinct 查询 */
            q.Select(a => new { a.Name }).Distinct().ToList();
            /*
             * SELECT DISTINCT [Users].[Name] AS [Name] FROM [Users] AS [Users]
             */

            ConsoleHelper.WriteLineAndReadKey();
        }
        public static void JoinQuery()
        {
            var user_city_province = context.Query<User>()
                                     .InnerJoin<City>((user, city) => user.CityId == city.Id)
                                     .InnerJoin<Province>((user, city, province) => city.ProvinceId == province.Id);

            //查出一个用户及其隶属的城市和省份的所有信息
            var view = user_city_province.Select((user, city, province) => new { User = user, City = city, Province = province }).Where(a => a.User.Id > 1).ToList();
            /*
             * SELECT [Users].[Id] AS [Id],[Users].[Name] AS [Name],[Users].[Gender] AS [Gender],[Users].[Age] AS [Age],[Users].[CityId] AS [CityId],[Users].[OpTime] AS [OpTime],[City].[Id] AS [Id0],[City].[Name] AS [Name0],[City].[ProvinceId] AS [ProvinceId],[Province].[Id] AS [Id1],[Province].[Name] AS [Name1] FROM [Users] AS [Users] INNER JOIN [City] AS [City] ON [Users].[CityId] = [City].[Id] INNER JOIN [Province] AS [Province] ON [City].[ProvinceId] = [Province].[Id] WHERE [Users].[Id] > 1
             */

            //也可以只获取指定的字段信息：UserId,UserName,CityName,ProvinceName
            user_city_province.Select((user, city, province) => new { UserId = user.Id, UserName = user.Name, CityName = city.Name, ProvinceName = province.Name }).Where(a => a.UserId > 1).ToList();
            /*
             * SELECT [Users].[Id] AS [UserId],[Users].[Name] AS [UserName],[City].[Name] AS [CityName],[Province].[Name] AS [ProvinceName] FROM [Users] AS [Users] INNER JOIN [City] AS [City] ON [Users].[CityId] = [City].[Id] INNER JOIN [Province] AS [Province] ON [City].[ProvinceId] = [Province].[Id] WHERE [Users].[Id] > 1
             */


            /* quick join and paging. */
            context.JoinQuery<User, City>((user, city) => new object[]
            {
                JoinType.LeftJoin, user.CityId == city.Id
            })
            .Select((user, city) => new { User = user, City = city })
            .Where(a => a.User.Id > -1)
            .OrderByDesc(a => a.User.Age)
            .TakePage(1, 20)
            .ToList();

            context.JoinQuery<User, City, Province>((user, city, province) => new object[]
            {
                JoinType.LeftJoin, user.CityId == city.Id,          /* 表 User 和 City 进行Left连接 */
                JoinType.LeftJoin, city.ProvinceId == province.Id   /* 表 City 和 Province 进行Left连接 */
            })
            .Select((user, city, province) => new { User = user, City = city, Province = province })   /* 投影成匿名对象 */
            .Where(a => a.User.Id > -1)     /* 进行条件过滤 */
            .OrderByDesc(a => a.User.Age)   /* 排序 */
            .TakePage(1, 20)                /* 分页 */
            .ToList();

            ConsoleHelper.WriteLineAndReadKey();
        }
        public static void AggregateQuery()
        {
            IQuery<User> q = context.Query<User>();

            q.Select(a => Sql.Count()).First();
            /*
             * SELECT COUNT(1) AS [C] FROM [Users] AS [Users] LIMIT 1 OFFSET 0
             */

            q.Select(a => new { Count = Sql.Count(), LongCount = Sql.LongCount(), Sum = Sql.Sum(a.Age), Max = Sql.Max(a.Age), Min = Sql.Min(a.Age), Average = Sql.Average(a.Age) }).First();
            /*
             * SELECT COUNT(1) AS [Count],COUNT(1) AS [LongCount],CAST(SUM([Users].[Age]) AS INTEGER) AS [Sum],MAX([Users].[Age]) AS [Max],MIN([Users].[Age]) AS [Min],CAST(AVG([Users].[Age]) AS REAL) AS [Average] FROM [Users] AS [Users] LIMIT 1 OFFSET 0
             */

            var count = q.Count();
            /*
             * SELECT COUNT(1) AS [C] FROM [Users] AS [Users]
             */

            var longCount = q.LongCount();
            /*
             * SELECT COUNT(1) AS [C] FROM [Users] AS [Users]
             */

            var sum = q.Sum(a => a.Age);
            /*
             * SELECT CAST(SUM([Users].[Age]) AS INTEGER) AS [C] FROM [Users] AS [Users]
             */

            var max = q.Max(a => a.Age);
            /*
             * SELECT MAX([Users].[Age]) AS [C] FROM [Users] AS [Users]
             */

            var min = q.Min(a => a.Age);
            /*
             * SELECT MIN([Users].[Age]) AS [C] FROM [Users] AS [Users]
             */

            var avg = q.Average(a => a.Age);
            /*
             * SELECT CAST(AVG([Users].[Age]) AS REAL) AS [C] FROM [Users] AS [Users]
             */

            ConsoleHelper.WriteLineAndReadKey();
        }
        public static void GroupQuery()
        {
            IQuery<User> q = context.Query<User>();

            IGroupingQuery<User> g = q.Where(a => a.Id > 0).GroupBy(a => a.Age);

            g = g.Having(a => true);

            g.Select(a => new { a.Age, Count = Sql.Count(), Sum = Sql.Sum(a.Age), Max = Sql.Max(a.Age), Min = Sql.Min(a.Age), Avg = Sql.Average(a.Age) }).ToList();
            /*
             * SELECT [Users].[Age] AS [Age],COUNT(1) AS [Count],CAST(SUM([Users].[Age]) AS INTEGER) AS [Sum],CAST(MAX([Users].[Age]) AS INTEGER) AS [Max],CAST(MIN([Users].[Age]) AS INTEGER) AS [Min],CAST(AVG([Users].[Age]) AS REAL) AS [Avg] FROM [Users] AS [Users] WHERE [Users].[Id] > 0 GROUP BY [Users].[Age] HAVING ([Users].[Age] > 1 AND COUNT(1) > 0)
             */

            ConsoleHelper.WriteLineAndReadKey();
        }
        /*复杂查询*/
        public static void ComplexQuery()
        {
            /*
             * 支持 select * from Users where CityId in (1,2,3)    --in一个数组
             * 支持 select * from Users where CityId in (select Id from City)    --in子查询
             * 支持 select * from Users exists (select 1 from City where City.Id=Users.CityId)    --exists查询
             * 支持 select (select top 1 CityName from City where Users.CityId==City.Id) as CityName, Users.Id, Users.Name from Users    --select子查询
             * 支持 select 
             *            (select count(*) from Users where Users.CityId=City.Id) as UserCount,     --总数
             *            (select max(Users.Age) from Users where Users.CityId=City.Id) as MaxAge,  --最大年龄
             *            (select avg(Users.Age) from Users where Users.CityId=City.Id) as AvgAge   --平均年龄
             *      from City
             *      --统计查询
             */

            IQuery<User> userQuery = context.Query<User>();
            IQuery<City> cityQuery = context.Query<City>();

            List<User> users = null;

            /* in 一个数组 */
            List<int> userIds = new List<int>() { 1, 2, 3 };
            users = userQuery.Where(a => userIds.Contains(a.Id)).ToList();  /* list.Contains() 方法组合就会生成 in一个数组 sql 语句 */
            /*
             * SELECT 
             *      [Users].[Gender] AS [Gender],[Users].[Age] AS [Age],[Users].[CityId] AS [CityId],[Users].[OpTime] AS [OpTime],[Users].[Id] AS [Id],[Users].[Name] AS [Name] 
             * FROM [Users] AS [Users] 
             * WHERE [Users].[Id] IN (1,2,3)
             */


            /* in 子查询 */
            users = userQuery.Where(a => cityQuery.Select(c => c.Id).ToList().Contains((int)a.CityId)).ToList();  /* IQuery<T>.ToList().Contains() 方法组合就会生成 in 子查询 sql 语句 */
            /*
             * SELECT 
             *      [Users].[Gender] AS [Gender],[Users].[Age] AS [Age],[Users].[CityId] AS [CityId],[Users].[OpTime] AS [OpTime],[Users].[Id] AS [Id],[Users].[Name] AS [Name] 
             * FROM [Users] AS [Users] 
             * WHERE [Users].[CityId] IN (SELECT [City].[Id] AS [C] FROM [City] AS [City])
             */


            /* IQuery<T>.Any() 方法组合就会生成 exists 子查询 sql 语句 */
            users = userQuery.Where(a => cityQuery.Where(c => c.Id == a.CityId).Any()).ToList();
            /*
             * SELECT 
             *      [Users].[Gender] AS [Gender],[Users].[Age] AS [Age],[Users].[CityId] AS [CityId],[Users].[OpTime] AS [OpTime],[Users].[Id] AS [Id],[Users].[Name] AS [Name] 
             * FROM [Users] AS [Users] 
             * WHERE Exists (SELECT '1' AS [C] FROM [City] AS [City] WHERE [City].[Id] = [Users].[CityId])
             */


            /* select 子查询 */
            var result = userQuery.Select(a => new
            {
                CityName = cityQuery.Where(c => c.Id == a.CityId).First().Name,
                User = a
            }).ToList();
            /*
             * SELECT 
             *      (SELECT [City].[Name] AS [C] FROM [City] AS [City] WHERE [City].[Id] = [Users].[CityId] LIMIT 1 OFFSET 0) AS [CityName],
             *      [Users].[Gender] AS [Gender],[Users].[Age] AS [Age],[Users].[CityId] AS [CityId],[Users].[OpTime] AS [OpTime],[Users].[Id] AS [Id],[Users].[Name] AS [Name] 
             * FROM [Users] AS [Users]
             */


            /* 统计 */
            var statisticsResult = cityQuery.Select(a => new
            {
                UserCount = userQuery.Where(u => u.CityId == a.Id).Count(),
                MaxAge = userQuery.Where(u => u.CityId == a.Id).Max(c => c.Age),
                AvgAge = userQuery.Where(u => u.CityId == a.Id).Average(c => c.Age),
            }).ToList();
            /*
             * SELECT 
             *      (SELECT COUNT(1) AS [C] FROM [Users] AS [Users] WHERE [Users].[CityId] = [City].[Id]) AS [UserCount],
             *      (SELECT MAX([Users].[Age]) AS [C] FROM [Users] AS [Users] WHERE [Users].[CityId] = [City].[Id]) AS [MaxAge],
             *      (SELECT CAST(AVG([Users].[Age]) AS REAL) AS [C] FROM [Users] AS [Users] WHERE [Users].[CityId] = [City].[Id]) AS [AvgAge] 
             * FROM [City] AS [City]
             */

            ConsoleHelper.WriteLineAndReadKey();
        }
        /* 贪婪加载导航属性 */
        public static void QueryWithNavigation()
        {
            object result = null;
            result = context.Query<User>().Include(a => a.City).ThenInclude(a => a.Province).ToList();
            result = context.Query<City>().Include(a => a.Province).IncludeMany(a => a.Users).AndWhere(a => a.Age >= 18).ToList();
            result = context.Query<Province>().IncludeMany(a => a.Cities).ThenIncludeMany(a => a.Users).ToList();

            result = context.Query<Province>().IncludeMany(a => a.Cities).ThenIncludeMany(a => a.Users).Where(a => a.Id > 0).TakePage(1, 20).ToList();

            result = context.Query<City>().IncludeMany(a => a.Users).AndWhere(a => a.Age > 18).ToList();

            ConsoleHelper.WriteLineAndReadKey();
        }


        public static void Insert()
        {
            //返回主键 Id
            int id = (int)context.Insert<User>(() => new User() { Name = "lu", Age = 18, Gender = Gender.Man, CityId = 1, OpTime = DateTime.Now });
            /*
             * INSERT INTO [Users]([Name],[Age],[Gender],[CityId],[OpTime]) VALUES('lu',18,1,1,DATETIME('NOW','LOCALTIME'));SELECT LAST_INSERT_ROWID()
             */

            User user = new User();
            user.Name = "lu";
            user.Age = 18;
            user.Gender = Gender.Man;
            user.CityId = 1;
            user.OpTime = DateTime.Now;

            //会自动将自增 Id 设置到 user 的 Id 属性上
            user = context.Insert(user);
            /*
             * String @P_0 = 'lu';
               Gender @P_1 = Man;
               Int32 @P_2 = 18;
               Int32 @P_3 = 1;
               DateTime @P_4 = '2016/8/6 22:03:42';
               INSERT INTO [Users]([Name],[Gender],[Age],[CityId],[OpTime]) VALUES(@P_0,@P_1,@P_2,@P_3,@P_4);SELECT LAST_INSERT_ROWID()
             */

            ConsoleHelper.WriteLineAndReadKey();
        }
        public static void Update()
        {
            context.Update<User>(a => a.Id == 1, a => new User() { Name = a.Name, Age = a.Age + 1, Gender = Gender.Man, OpTime = DateTime.Now });
            /*
             * UPDATE [Users] SET [Name]=[Users].[Name],[Age]=([Users].[Age] + 1),[Gender]=1,[OpTime]=DATETIME('NOW','LOCALTIME') WHERE [Users].[Id] = 1
             */

            //批量更新
            //给所有女性年轻 1 岁
            context.Update<User>(a => a.Gender == Gender.Woman, a => new User() { Age = a.Age - 1, OpTime = DateTime.Now });
            /*
             * UPDATE [Users] SET [Age]=([Users].[Age] - 1),[OpTime]=DATETIME('NOW','LOCALTIME') WHERE [Users].[Gender] = 2
             */

            User user = new User();
            user.Id = 1;
            user.Name = "lu";
            user.Age = 28;
            user.Gender = Gender.Man;
            user.OpTime = DateTime.Now;

            context.Update(user); //会更新所有映射的字段
            /*
             * String @P_0 = 'lu';
               Gender @P_1 = Man;
               Int32 @P_2 = 28;
               Nullable<Int32> @P_3 = NULL;
               DateTime @P_4 = '2016/8/6 22:05:02';
               Int32 @P_5 = 1;
               UPDATE [Users] SET [Name]=@P_0,[Gender]=@P_1,[Age]=@P_2,[CityId]=@P_3,[OpTime]=@P_4 WHERE [Users].[Id] = @P_5
             */


            /*
             * 支持只更新属性值已变的属性
             */

            context.TrackEntity(user);//在上下文中跟踪实体
            user.Name = user.Name + "1";
            context.Update(user);//这时只会更新被修改的字段
            /*
             * String @P_0 = 'lu1';
               Int32 @P_1 = 1;
               UPDATE [Users] SET [Name]=@P_0 WHERE [Users].[Id] = @P_1
             */

            ConsoleHelper.WriteLineAndReadKey();
        }
        public static void Delete()
        {
            context.Delete<User>(a => a.Id == 1);
            /*
             * DELETE FROM [Users] WHERE [Users].[Id] = 1
             */

            //批量删除
            //删除所有不男不女的用户
            context.Delete<User>(a => a.Gender == null);
            /*
             * DELETE FROM [Users] WHERE [Users].[Gender] IS NULL
             */

            User user = new User();
            user.Id = 1;
            context.Delete(user);
            /*
             * Int32 @P_0 = 1;
               DELETE FROM [Users] WHERE [Users].[Id] = @P_0
             */

            ConsoleHelper.WriteLineAndReadKey(1);
        }

        public static void Method()
        {
            IQuery<User> q = context.Query<User>();

            var space = new char[] { ' ' };

            DateTime startTime = DateTime.Now;
            DateTime endTime = DateTime.Now.AddDays(1);

            var ret = q.Select(a => new
            {
                Id = a.Id,

                String_Length = (int?)a.Name.Length,//LENGTH([Users].[Name])
                Substring = a.Name.Substring(0),//SUBSTR([Users].[Name],0 + 1)
                Substring1 = a.Name.Substring(1),//SUBSTR([Users].[Name],1 + 1)
                Substring1_2 = a.Name.Substring(1, 2),//SUBSTR([Users].[Name],1 + 1,2)
                ToLower = a.Name.ToLower(),//LOWER([Users].[Name])
                ToUpper = a.Name.ToUpper(),//UPPER([Users].[Name])
                IsNullOrEmpty = string.IsNullOrEmpty(a.Name),//CASE WHEN ([Users].[Name] IS NULL OR [Users].[Name] = '') THEN 1 ELSE 0 END = 1
                Contains = (bool?)a.Name.Contains("s"),//[Users].[Name] LIKE '%' || 's' || '%'
                StartsWith = (bool?)a.Name.StartsWith("s"),//[Users].[Name] LIKE 's' || '%'
                EndsWith = (bool?)a.Name.EndsWith("s"),//[Users].[Name] LIKE '%' || 's'
                Trim = a.Name.Trim(),//TRIM([Users].[Name])
                TrimStart = a.Name.TrimStart(space),//LTRIM([Users].[Name])
                TrimEnd = a.Name.TrimEnd(space),//RTRIM([Users].[Name])
                Replace = a.Name.Replace("l", "L"),

                DiffYears = Sql.DiffYears(startTime, endTime),//(CAST(STRFTIME('%Y',@P_0) AS INTEGER) - CAST(STRFTIME('%Y',@P_1) AS INTEGER))
                DiffMonths = Sql.DiffMonths(startTime, endTime),//((CAST(STRFTIME('%Y',@P_0) AS INTEGER) - CAST(STRFTIME('%Y',@P_1) AS INTEGER)) * 12 + (CAST(STRFTIME('%m',@P_0) AS INTEGER) - CAST(STRFTIME('%m',@P_1) AS INTEGER)))
                DiffDays = Sql.DiffDays(startTime, endTime),//CAST((JULIANDAY(@P_0) - JULIANDAY(@P_1)) AS INTEGER)
                DiffHours = Sql.DiffHours(startTime, endTime),//CAST((JULIANDAY(@P_0) - JULIANDAY(@P_1)) * 24 AS INTEGER)
                DiffMinutes = Sql.DiffMinutes(startTime, endTime),//CAST((JULIANDAY(@P_0) - JULIANDAY(@P_1)) * 1440 AS INTEGER)
                DiffSeconds = Sql.DiffSeconds(startTime, endTime),//CAST((JULIANDAY(@P_0) - JULIANDAY(@P_1)) * 86400 AS INTEGER)
                //DiffMilliseconds = Sql.DiffMilliseconds(startTime, endTime),//不支持 Millisecond
                //DiffMicroseconds = Sql.DiffMicroseconds(startTime, endTime),//不支持 Microseconds

                AddYears = startTime.AddYears(1),//DATETIME(@P_0,'+' || 1 || ' years')
                AddMonths = startTime.AddMonths(1),//DATETIME(@P_0,'+' || 1 || ' months')
                AddDays = startTime.AddDays(1),//DATETIME(@P_0,'+' || 1 || ' days')
                AddHours = startTime.AddHours(1),//DATETIME(@P_0,'+' || 1 || ' hours')
                AddMinutes = startTime.AddMinutes(2),//DATETIME(@P_0,'+' || 2 || ' minutes')
                AddSeconds = startTime.AddSeconds(120),//DATETIME(@P_0,'+' || 120 || ' seconds')
                //AddMilliseconds = startTime.AddMilliseconds(2000),//不支持

                Now = DateTime.Now,//DATETIME('NOW','LOCALTIME')
                UtcNow = DateTime.UtcNow,//DATETIME()
                Today = DateTime.Today,//DATE('NOW','LOCALTIME')
                Date = DateTime.Now.Date,//DATE('NOW','LOCALTIME')
                Year = DateTime.Now.Year,//CAST(STRFTIME('%Y',DATETIME('NOW','LOCALTIME')) AS INTEGER)
                Month = DateTime.Now.Month,//CAST(STRFTIME('%m',DATETIME('NOW','LOCALTIME')) AS INTEGER)
                Day = DateTime.Now.Day,//CAST(STRFTIME('%d',DATETIME('NOW','LOCALTIME')) AS INTEGER)
                Hour = DateTime.Now.Hour,//CAST(STRFTIME('%H',DATETIME('NOW','LOCALTIME')) AS INTEGER)
                Minute = DateTime.Now.Minute,//CAST(STRFTIME('%M',DATETIME('NOW','LOCALTIME')) AS INTEGER)
                Second = DateTime.Now.Second,//CAST(STRFTIME('%S',DATETIME('NOW','LOCALTIME')) AS INTEGER)
                Millisecond = DateTime.Now.Millisecond,//@P_2 直接计算 DateTime.Now.Millisecond 的值 
                DayOfWeek = DateTime.Now.DayOfWeek,//CAST(STRFTIME('%w',DATETIME('NOW','LOCALTIME')) AS INTEGER)

                Byte_Parse = byte.Parse("1"),//CAST('1' AS INTEGER)
                Int_Parse = int.Parse("1"),//CAST('1' AS INTEGER)
                Int16_Parse = Int16.Parse("11"),//CAST('11' AS INTEGER)
                Long_Parse = long.Parse("2"),//CAST('2' AS INTEGER)
                Double_Parse = double.Parse("3.1"),//CAST('3.1' AS REAL)
                Float_Parse = float.Parse("4.1"),//CAST('4.1' AS REAL)
                //Decimal_Parse = decimal.Parse("5"),//不支持
                //Guid_Parse = Guid.Parse("D544BC4C-739E-4CD3-A3D3-7BF803FCE179"),//不支持 'D544BC4C-739E-4CD3-A3D3-7BF803FCE179'

                Bool_Parse = bool.Parse("1"),//CAST('1' AS INTEGER)
                DateTime_Parse = DateTime.Parse("2014-01-01"),//DATETIME('2014-01-01')

                B = a.Age == null ? false : a.Age > 1, //三元表达式
                CaseWhen = Case.When(a.Id > 100).Then(1).Else(0) //case when
            }).ToList();

            ConsoleHelper.WriteLineAndReadKey();
        }

        public static void ExecuteCommandText()
        {
            List<User> users = context.SqlQuery<User>("select * from Users where Age > @age", DbParam.Create("@age", 12)).ToList();

            int rowsAffected = context.Session.ExecuteNonQuery("update Users set name=@name where Id = 1", DbParam.Create("@name", "Chloe"));

            /* 
             * 执行存储过程:
             * User user = context.SqlQuery<User>("Proc_GetUser", CommandType.StoredProcedure, DbParam.Create("@id", 1)).FirstOrDefault();
             * rowsAffected = context.Session.ExecuteNonQuery("Proc_UpdateUserName", CommandType.StoredProcedure, DbParam.Create("@name", "Chloe"));
             */

            ConsoleHelper.WriteLineAndReadKey();
        }

        public static void DoWithTransactionEx()
        {
            context.UseTransaction(() =>
            {
                context.Update<User>(a => a.Id == 1, a => new User() { Name = a.Name, Age = a.Age + 1, Gender = Gender.Man, OpTime = DateTime.Now });
                context.Delete<User>(a => a.Id == 1024);
            });

            ConsoleHelper.WriteLineAndReadKey();
        }
        public static void DoWithTransaction()
        {
            using (ITransientTransaction tran = context.BeginTransaction())
            {
                /* do some things here */
                context.Update<User>(a => a.Id == 1, a => new User() { Name = a.Name, Age = a.Age + 1, Gender = Gender.Man, OpTime = DateTime.Now });
                context.Delete<User>(a => a.Id == 1024);

                tran.Commit();
            }

            ConsoleHelper.WriteLineAndReadKey();
        }
    }
}
