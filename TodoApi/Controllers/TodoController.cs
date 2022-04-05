using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using TodoApi.Models;

namespace TodoApi.Controllers
{

    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class TodoController : ControllerBase
    {
        private readonly TodoContext _context;

        public TodoController(TodoContext context)
        {
            _context = context;
        }

        [HttpGet]
        public ActionResult<List<TodoItem>> GetAll([FromQuery] int limit = 5)
        {
            return _context.TodoItems.Take(limit).Select(i => (TodoItem)i).ToList();
        }

        [HttpGet("{id}", Name = "GetTodo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<TodoItem> GetById(long id)
        {
            var item = _context.TodoItems.Find(id);

            if (item == null)
            {
                return NotFound();
            }

            return new TodoItem
            {
                Id = item.Id,
                IsComplete = item.IsComplete,
                Name = item.Name
            };
        }

        /// <summary>
        /// Creates a TodoItem.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /Todo
        ///     {
        ///        "id": 1,
        ///        "name": "Item1",
        ///        "isComplete": true
        ///     }
        ///
        /// </remarks>
        /// <param name="input"></param>
        /// <returns>A newly created TodoItem</returns>
        /// <response code="201">Returns the newly created item</response>
        /// <response code="400">If the item is null</response>            
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<TodoItem> Create(TodoItem input)
        {
            var item = new TodoItemDBModel
            {
                Id = input.Id,
                Name = input.Name,
                IsComplete = input.IsComplete
            };

            _context.TodoItems.Add(item);
            _context.SaveChanges();

            input.Id = item.Id;

            return CreatedAtRoute("GetTodo", new { id = item.Id }, input);
        }


        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Update(long id, TodoItem item)
        {
            if (item == null || item.Id != id)
            {
                return BadRequest();
            }

            var todo = _context.TodoItems.Find(id);

            if (todo == null)
            {
                return NotFound();
            }

            todo.IsComplete = item.IsComplete;
            todo.Name = item.Name;

            _context.TodoItems.Update(todo);
            _context.SaveChanges();

            return NoContent();
        }


        /// <summary>
        /// Deletes a specific TodoItem.
        /// </summary>
        /// <param name="id"></param>        
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Delete(long id)
        {
            var todo = _context.TodoItems.Find(id);

            if (todo == null)
            {
                return NotFound();
            }

            _context.Entry(todo).Collection(i => i.Comments).Load();

            _context.TodoItems.Remove(todo);
            _context.SaveChanges();

            return NoContent();
        }

        [HttpGet("{todoId}/comments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<List<Comment>> GetComments(long todoId)
        {
            var item = _context.TodoItems.Find(todoId);

            if (item == null)
                return NotFound();

            var comments = _context.Entry(item).Collection(i => i.Comments).Query().ToList();

            return Ok(comments);
        }

        [HttpGet("{todoId}/comments/{commentId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Comment> GetCommentById(long todoId, long commentId)
        {
            var item = _context.TodoItems.Find(todoId);

            if (item == null)
                return NotFound();

            var comment = _context.Entry(item).Collection(i => i.Comments).Query().FirstOrDefault(c => c.Id == commentId);

            if (comment == null)
                return NotFound();

            return Ok(comment);
        }

        [HttpPost("{todoId}/comments")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<TodoItem> CreateComment(long todoId, CommentInput input)
        {
            var comment = new Comment
            {
                TodoItemId = todoId,
                AuthorId = input.AuthorId,
                Message = input.Message
            };

            var item = _context.TodoItems.Find(todoId);

            if (item == null)
                return NotFound();

             _context.Entry(item).Collection(i => i.Comments).Load();

            item.Comments.Add(comment);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetCommentById), new { todoId, commentId = comment.Id }, comment);
        }

        [HttpDelete("{todoId}/comments/{commentId}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DeleteComment(long todoId, long commentId)
        {
            var item = _context.TodoItems.Find(todoId);

            if (item == null)
                return NotFound();

            _context.Entry(item).Collection(i => i.Comments).Load();

            var commend = item.Comments.FirstOrDefault(c => c.Id == commentId);

            if (commend == null)
                return NotFound();

            item.Comments.Remove(commend);
            _context.SaveChanges();

            return NoContent();
        }

        [HttpPost("reset")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Reset()
        {
            _context.Database.EnsureDeleted();

            return NoContent();
        }
    }
}