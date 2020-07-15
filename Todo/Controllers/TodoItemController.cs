using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Todo.Data;
using Todo.Data.Entities;
using Todo.EntityModelMappers.TodoItems;
using Todo.Models.TodoItems;
using Todo.Models.TodoLists;
using Todo.Services;

namespace Todo.Controllers
{
    [Authorize]
    public class TodoItemController : Controller
    {
        private readonly ApplicationDbContext dbContext;

        public TodoItemController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Create(int todoListId)
        {
            var todoList = dbContext.SingleTodoList(todoListId);
            var fields = TodoItemCreateFieldsFactory.Create(todoList, User.Id());
            return View(fields);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TodoItemCreateFields fields)
        {
            if (!ModelState.IsValid) { return View(fields); }

            var item = new TodoItem(fields.TodoListId, fields.ResponsiblePartyId, fields.Title, fields.Importance);

            await dbContext.AddAsync(item);
            await dbContext.SaveChangesAsync();

            return RedirectToListDetail(fields.TodoListId);
        }

        [HttpGet]
        public IActionResult Edit(int todoItemId)
        {
            var todoItem = dbContext.SingleTodoItem(todoItemId);
            var fields = TodoItemEditFieldsFactory.Create(todoItem);
            return View(fields);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TodoItemEditFields fields)
        {
            if (!ModelState.IsValid) { return View(fields); }

            var todoItem = dbContext.SingleTodoItem(fields.TodoItemId);

            TodoItemEditFieldsFactory.Update(fields, todoItem);

            dbContext.Update(todoItem);
            await dbContext.SaveChangesAsync();

            return RedirectToListDetail(todoItem.TodoListId);
        }
        /// <summary>
        /// Update the rank of an item
        /// probably not the most efficient way.  I.e. redirect to the view
        /// possibly should return a result back to the calling JS
        /// should validate anti forgery token
        /// </summary>
        /// <param name="todoListId"></param>
        /// <param name="todoItemId"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<IActionResult> UpdateRank([FromBody] TodoItemEditFields fields)
        {
            if (fields.Rank <= 0 || fields.Rank > 9999)
            {
                return ValidationProblem();
            }
            else
            {
                var todoItem = dbContext.SingleTodoItem(fields.TodoItemId);
                todoItem.Rank = fields.Rank;

                dbContext.Update(todoItem);
                try
                {
                    await dbContext.SaveChangesAsync();
                }
                catch (System.Exception)
                {

                    return NotFound();
                }
            }
            return NoContent();
            //return RedirectToListDetail(1);
        }

        private RedirectToActionResult RedirectToListDetail(int fieldsTodoListId)
        {
            return RedirectToAction("Detail", "TodoList", new {todoListId = fieldsTodoListId});
        }
    }
}