﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using jewelry.Data;
using jewelry.Models;
using Microsoft.AspNetCore.Authorization;

namespace jewelry.Controllers
{
    public class OrdersController : Controller
    {
        private readonly jewelryContext _context;

        public OrdersController(jewelryContext context)
        {
            _context = context;
        }

         [HttpPost]
        public IActionResult OrderForm([Bind("Id,Date,TotalPrice,Payment,Sended")] Order order,[Bind("PhoneNumber,State,City,Street,HouseNumber,ApartmentNumber,PostalCode")] Address address)
         {
            if (User.Identity.IsAuthenticated)
            {
                int userId = Int32.Parse(User.Claims.Where(c => c.Type.Equals("UserId")).Select(c => c.Value).SingleOrDefault());

                //check if the user still exist 
                if (_context.User.Find(userId) == null)
                {
                    ViewData["UserProblem"] = "error";
                    return PartialView();
                }
                int cartid = Int32.Parse(User.Claims.Where(c => c.Type.Equals("cartId")).Select(c => c.Value).SingleOrDefault());
                Cart cart = _context.Cart.Include(a => a.ProductCartId).Where(a => a.Id.Equals(cartid)).First();
                foreach (var procart in cart.ProductCartId)
                {
                    var pro = _context.Product.Find(procart.ProductId);
                    pro.StoreQuantity -= 1;
                }
                order.Sended = false;
                order.UserId = userId;
                order.Date = DateTime.Now;
                order.TotalPrice = cart.TotalPrice;
                _context.Address.Add(address);
                _context.SaveChanges();
                order.AddressId = address.Id;

                if (ModelState.IsValid)
                {
                    order.AddressId = address.Id;
                    _context.Order.Add(order);
                    _context.SaveChanges();
                    //clean cart await
                    List<ProductCart> productCarts = _context.ProductCart.Where(a => a.CartId.Equals(cartid)).ToList();
                    if (productCarts != null)
                    {
                        foreach (ProductCart productcart in productCarts)
                        {
                            Product product = _context.Product.Find(productcart.ProductId);
                            if (product != null)
                            {
                                product.StoreQuantity -= 1;
                            }
                            _context.ProductCart.Remove(productcart);
                        }
                    }
                    if (cart != null)
                    {
                        cart.TotalPrice = 0;
                    }
                    _context.SaveChanges();
                    ViewData["orderid"] = order.Id;
                    ViewData["arrivalDate"] = DateTime.Now.AddDays(7).Date;
                    return PartialView("OrderDone");

                }
                return PartialView();
            }
            else return NotFound();

          
         }




        //OrderDone Get
        [Authorize]
        public IActionResult OrderDone(int id)
        {
            ViewData["orderid"] = id;
            ViewData["arrivalDate"] = DateTime.Now.AddDays(7).Date;
            return PartialView();
        }


        //order get:
        [Authorize]
        [HttpGet]
        public IActionResult OrderForm(int total, int cartid,float currenttotalprice)
        {
            //finding all the product, if the quantity are different than theres missing product
            Cart cart = _context.Cart.Include(a => a.ProductCartId).Where(a => a.Id.Equals(cartid)).First();
            if (cart != null)
            {
                int quantity = 0;
                double totalPrice = 0;
                List<ProductCart> productscarts = cart.ProductCartId;
                quantity = productscarts.Count();
                if (quantity != total || quantity == 0 || currenttotalprice!= cart.TotalPrice) // אם שינוי את הכמות תוך כדי
                {
                    return RedirectToAction("MyCart", "Carts",new { id = cartid });
/*                    PartialViewResult partial = PartialView((new CartsController(_context)).MyCart(cartid));
*//*                    return ;
*/                }
                foreach (var productCart in productscarts)
                {
                    Product product = _context.Product.Find(productCart.ProductId);

                    if (product == null)//אם מחקו את המוצר תוך כדי
                    {
                      return View("MyCart", "Carts");
                     }
                    
                    double price = product.Price;
                    totalPrice += price * productCart.Quantity;

                }
                if (totalPrice == 0)
                {
                    return View("MyCart", "Carts");
                }
                cart.TotalPrice = totalPrice;
                _context.Update(cart);
                ViewData["totalPrice"] = totalPrice;
                ViewData["productCartList"] = productscarts;
                return PartialView();
                //אחר כך צריך לעשות creat order ושם להוריד את המלאי
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }

        }

        public IActionResult facebook()
        {
            return View();
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            return View(await _context.Order.ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Date,TotalPrice,Payment,Sended")] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }



        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Date,TotalPrice,Payment,Sended")] Order order)
        {
            if (id != order.Id)
            if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Order.FindAsync(id);
            _context.Order.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Order.Any(e => e.Id == id);
        }
    }
}
