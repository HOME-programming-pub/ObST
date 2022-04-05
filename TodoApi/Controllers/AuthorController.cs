using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Models;

namespace TodoApi.Controllers
{

    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthorController : ControllerBase
    {
        private readonly TodoContext _context;

        public AuthorController(TodoContext context)
        {
            _context = context;
        }

        [HttpGet]
        public ActionResult<List<Author>> GetAll()
        {
            return _context.Authors.ToList();
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Author> GetById(long id)
        {
            var item = _context.Authors.Find(id);

            if (item == null)
            {
                return NotFound();
            }

            return item;
        }
      
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<Author> Create(Author item)
        {
            _context.Authors.Add(item);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }


        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Update(long id, Author item)
        {
            if (item == null || item.Id != id)
            {
                return BadRequest();
            }

            var author = _context.Authors.Find(id);

            if (author == null)
            {
                return NotFound();
            }

            author.Name = item.Name;

            _context.Authors.Update(author);
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
            var author = _context.Authors.Find(id);

            if (author == null)
            {
                return NotFound();
            }

            _context.Authors.Remove(author);
            _context.SaveChanges();

            return NoContent();
        }
    }
}
