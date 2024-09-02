using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace HW_2_EF_Core
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }

        public ICollection<ProductOrder> ProductOrders { get; set; } = new List<ProductOrder>();
    }

    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public ICollection<ProductOrder> ProductOrders { get; set; } = new List<ProductOrder>();
    }

    public class ProductOrder
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
    }


    public class ApplicationContext : DbContext
    {
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<ProductOrder> ProductOrders { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=DESKTOP-GVAO16L;Database=Shop;Trusted_Connection=True;TrustServerCertificate=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductOrder>()
                .HasKey(po => new { po.ProductId, po.OrderId });

            modelBuilder.Entity<ProductOrder>()
                .HasOne(po => po.Product)
                .WithMany(p => p.ProductOrders)
                .HasForeignKey(po => po.ProductId);

            modelBuilder.Entity<ProductOrder>()
                .HasOne(po => po.Order)
                .WithMany(o => o.ProductOrders)
                .HasForeignKey(po => po.OrderId);
        }
    }


    public class OrderService
    {
        private readonly ApplicationContext _context;

        public OrderService(ApplicationContext context)
        {
            _context = context;
        }

        public void AddOrder(Order order)
        {
            _context.Orders.Add(order);
            _context.SaveChanges();
        }

        public void RemoveOrder(int orderId)
        {
            var order = _context.Orders.Find(orderId);
            if (order != null)
            {
                _context.Orders.Remove(order);
                _context.SaveChanges();
            }
        }

        public Order? GetOrder(int orderId)
        {
            return _context.Orders
                .Include(o => o.ProductOrders)
                .ThenInclude(po => po.Product)
                .FirstOrDefault(o => o.Id == orderId);
        }

        public IEnumerable<Order> GetAllOrders()
        {
            return _context.Orders
                .Include(o => o.ProductOrders)
                .ThenInclude(po => po.Product)
                .ToList();
        }
    }


    class Program
    {
        static void Main()
        {
            using (var db = new ApplicationContext())
            {
                db.Database.EnsureCreated();

                if (!db.Products.Any())
                {
                    db.Products.AddRange(new[]
                    {
                        new Product { Name = "Product1", Price = 10.0m },
                        new Product { Name = "Product2", Price = 20.0m }
                    });
                    db.SaveChanges();
                }

                var orderService = new OrderService(db);

                var order = new Order
                {
                    OrderDate = DateTime.Now,
                    ProductOrders = new List<ProductOrder>
                    {
                        new ProductOrder { ProductId = 1, OrderId = 1 },
                        new ProductOrder { ProductId = 2, OrderId = 1 }
                    }
                };

                orderService.AddOrder(order);

                var orders = orderService.GetAllOrders();
                foreach (var o in orders)
                {
                    Console.WriteLine($"Order ID: {o.Id}, Date: {o.OrderDate}");
                    foreach (var po in o.ProductOrders)
                    {
                        Console.WriteLine($"Product: {po.Product.Name}, Price: {po.Product.Price}");
                    }
                }

                //orderService.RemoveOrder(1);
            }
        }
    }
}
