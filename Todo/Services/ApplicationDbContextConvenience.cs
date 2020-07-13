using System.Linq;
using Microsoft.EntityFrameworkCore;
using Todo.Data;
using Todo.Data.Entities;

namespace Todo.Services
{
    public static class ApplicationDbContextConvenience
    {
        public static IQueryable<TodoList> RelevantTodoLists(this ApplicationDbContext dbContext, string userId)
        {

            //var query = from l in dbContext.Set<TodoList>()
            //            join i in dbContext.Set<TodoItem>()
            //                on l.TodoListId equals i.TodoListId into grouping
            //            from p in grouping.DefaultIfEmpty()
            //            where p.ResponsiblePartyId == userId || l.Owner.Id == userId
            //            select l;

            // possibly not the most efficient route
            // couldn't get a  working version starting at the TodoList level
            return dbContext.TodoItems
                .Include(ti => ti.TodoList)
                    .ThenInclude(tl => tl.Owner)
                .Where(i => i.ResponsiblePartyId == userId || i.TodoList.Owner.Id == userId)
                .Select(x => x.TodoList)
                .Distinct()
                .Include(tl => tl.Owner)
                .Include(tl => tl.Items);
                                          

        }

        public static TodoList SingleTodoList(this ApplicationDbContext dbContext, int todoListId)
        {
            return dbContext.TodoLists.Include(tl => tl.Owner)
                .Include(tl => tl.Items)
                .ThenInclude(ti => ti.ResponsibleParty)
                .Single(tl => tl.TodoListId == todoListId);
        }

        public static TodoItem SingleTodoItem(this ApplicationDbContext dbContext, int todoItemId)
        {
            return dbContext.TodoItems.Include(ti => ti.TodoList).Single(ti => ti.TodoItemId == todoItemId);
        }
    }
}