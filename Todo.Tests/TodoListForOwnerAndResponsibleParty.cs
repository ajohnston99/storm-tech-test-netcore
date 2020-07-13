using System;
using System.Data.Common;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Todo.Data;
using Todo.Data.Entities;
using Todo.EntityModelMappers.TodoItems;
using Todo.Models.TodoItems;
using Todo.Services;
using Xunit;

namespace Todo.Tests
{
    public class TodoListForOwnerAndResponsibleParty 
    {
        private const string InMemoryConnectionString = "DataSource=:memory:";
        private readonly SqliteConnection _connection;

        protected readonly ApplicationDbContext dbContext;

        private IdentityUser firstUser;
        private IdentityUser secondUser;
        private IdentityUser thirdUser;

        //TODO:setup db context & test methods for retrieving to do lists for owner and responsible party
        public TodoListForOwnerAndResponsibleParty()
        {
            _connection = new SqliteConnection(InMemoryConnectionString);
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(_connection)
                    .Options;
            dbContext = new ApplicationDbContext(options);
            dbContext.Database.EnsureCreated();

            SetupUserAndLists();
        }

        private void SetupUserAndLists()
        {
            //first user only has lists and items they own and are responsible for
            //second user has has one item that user 3 is responsible for
            //third user has one item in the second user's list that they are responsible for

            firstUser = new IdentityUser("user1@example.com");
            secondUser = new IdentityUser("user2@example.com");
            thirdUser = new IdentityUser("user3@example.com");

            Assert.NotEqual(firstUser.Id, secondUser.Id);
            Assert.NotEqual(firstUser.Id, thirdUser.Id);
            Assert.NotEqual(secondUser.Id, thirdUser.Id);

            var todoListFirstUserShopping = new TestTodoListBuilder(firstUser, "shopping")
            .WithItem("bread", Importance.High, firstUser.Id)
            .WithItem("cheese", Importance.High, firstUser.Id)
            .Build();

            dbContext.Add(todoListFirstUserShopping);
            dbContext.SaveChanges();

            var todoListFirstUserDIY = new TestTodoListBuilder(firstUser, "DIY")
            .WithItem("paint door", Importance.High, firstUser.Id)
            .WithItem("tidy garden", Importance.High, firstUser.Id)
            .Build();

            dbContext.Add(todoListFirstUserDIY);
            dbContext.SaveChanges();

            var todoListSecondUser= new TestTodoListBuilder(secondUser, "task list U2L1")
            .WithItem("Second user list 1 Task 1", Importance.High, secondUser.Id)
            .WithItem("Second user list 1 Task 2", Importance.High, secondUser.Id)
            .Build();

            dbContext.Add(todoListSecondUser);
            dbContext.SaveChanges();

            var todoListThirdUser = new TestTodoListBuilder(thirdUser, "task list U3L1")
            .WithItem("Third user list 1 Task 1", Importance.High, thirdUser.Id)
            .WithItem("Third user list 1 Task 2", Importance.High, thirdUser.Id)
            .WithItem("Third user list 1 Task 3", Importance.High, thirdUser.Id)
            .Build();

            dbContext.Add(todoListThirdUser);
            dbContext.SaveChanges();

            var todoListSecondUserSecondList = new TestTodoListBuilder(secondUser, "task list U2L2")
            .WithItem("Second user list 2 Task 1", Importance.High, secondUser.Id)
            .WithItem("Second user list 2 Task 2", Importance.High, secondUser.Id)
            .WithItem("Second user list 2 Task 3", Importance.High, thirdUser.Id) //different resposible party
            .Build();

            dbContext.Add(todoListSecondUserSecondList);
            dbContext.SaveChanges();

        }

        [Fact]
        public void UsersNoSharedTasksReturnsCorrectValue()
        {
            var toDoListForUser = dbContext.RelevantTodoLists(firstUser.Id);
            Assert.Equal(2, toDoListForUser.Count());
            foreach (var item in toDoListForUser)
            {
                var o = item.Owner;
                var task = item.Items.Where(t => t.ResponsiblePartyId == firstUser.Id).FirstOrDefault();
                Assert.True(o.Id == firstUser.Id || task?.ResponsiblePartyId == firstUser.Id);

            }
        }

        [Fact]
        public void UsersWithDifferentResponsibleUserReturnsCorrectValue()
        {
            var toDoListForUser = dbContext.RelevantTodoLists(thirdUser.Id);
            // should return the thirdUser list and one from secondUser
            Assert.Equal(2, toDoListForUser.Count());
            //Assert.Equal(thirdUser.Id, toDoListForUser.First().Owner.Id);

            foreach (var item in toDoListForUser)
            {
                var o = item.Owner;
                var task = item.Items.Where(t => t.ResponsiblePartyId == thirdUser.Id).FirstOrDefault();
                Assert.True(o.Id == thirdUser.Id || task?.ResponsiblePartyId == thirdUser.Id);

            }
        }

        [Fact]
        public void UsersResponsibleUserReturnsCorrectValue()
        {
            var toDoListForUser = dbContext.RelevantTodoLists(thirdUser.Id);
            // this user is responsible for a task in user 2's list
            Assert.Equal(2, toDoListForUser.Count());
            //check that this user is either the owner of the list or a responsible party for at least one task
            foreach (var item in toDoListForUser)
            {
                var o = item.Owner;
                var task = item.Items.Where(t => t.ResponsiblePartyId == thirdUser.Id).FirstOrDefault();
                Assert.True(o.Id == thirdUser.Id || task?.ResponsiblePartyId == thirdUser.Id);

            }
            
        }

    }
}
