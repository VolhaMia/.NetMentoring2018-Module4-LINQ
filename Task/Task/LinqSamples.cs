// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
//Copyright (C) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using SampleSupport;
using Task.Data;

// Version Mad01

namespace SampleQueries
{
	[Title("LINQ Module")]
	[Prefix("Linq")]
	public class LinqSamples : SampleHarness
	{

		private DataSource dataSource = new DataSource();

	    [Category("Task")]
	    [Title("Task 001")]
	    [Description("Выдайте список всех клиентов, чей суммарный оборот (сумма всех заказов) превосходит некоторую величину X")]
        public void Linq001()
	    {
	        decimal x = 3000;
	        var customersList = dataSource.Customers
	            .Select(c => new {CustomerId = c.CustomerID, TotalSum = c.Orders.Sum(o => o.Total)})
	            .Where(c => c.TotalSum > x);

	        ObjectDumper.Write($"More than {x}");
	        foreach (var customer in customersList)
	        {
	            ObjectDumper.Write(
	                string.Format($"CustomerId = {customer.CustomerId} TotalSum = {customer.TotalSum}\n"));
	        }

	        x = 6000;
	        ObjectDumper.Write($"More than {x}");
	        foreach (var customer in customersList)
	        {
	            ObjectDumper.Write(
	                string.Format($"CustomerId = {customer.CustomerId} TotalSum = {customer.TotalSum}\n"));
	        }
	    }

	    [Category("Task")]
	    [Title("Task 002")]
	    [Description("Для каждого клиента составьте список поставщиков, находящихся в той же стране и том же городе. Сделайте задания с использованием группировки и без")]
	    public void Linq002()
	    {
	        var suppliers = dataSource.Customers
	            .Select(c => new
	            {
	                CustomerID = c.CustomerID,
	                Suppliers = dataSource.Suppliers.Where(s => s.City == c.City && s.Country == c.Country)
	            });

	        ObjectDumper.Write("Without grouping\n");
	        foreach (var customer in suppliers)
	        {
	            ObjectDumper.Write($"CustomerId: {customer.CustomerID} " +
	                               $"List of suppliers: {string.Join(", ", customer.Suppliers.Select(s => s.SupplierName))}");
	        }

	        var result = dataSource.Customers.GroupJoin(dataSource.Suppliers,
	            c => new { c.City, c.Country },
	            s => new { s.City, s.Country },
	            (c, ss) => new { CustomerID = c.CustomerID, Suppliers = ss.Select(s => s.SupplierName) });

	        ObjectDumper.Write("With  grouping:\n");
	        foreach (var c in result)
	        {
	            ObjectDumper.Write($"CustomerId: {c.CustomerID} " +
	                               $"List of suppliers: {string.Join(", ", c.Suppliers)}");
	        }
	    }

	    [Category("Task")]
	    [Title("Task 003")]
	    [Description("Найдите всех клиентов, у которых были заказы, превосходящие по сумме величину X")]
	    public void Linq003()
	    {
	        decimal x = 6000;
	        var customers = dataSource.Customers.Where(c => c.Orders.Any(s => s.Total > x));

	        foreach (var c in customers)
	        {
	            ObjectDumper.Write(c);
	        }
	    }

	    [Category("Task")]
	    [Title("Task 004")]
	    [Description("Выдайте список клиентов с указанием, начиная с какого месяца какого года они стали клиентами (принять за таковые месяц и год самого первого заказа)")]
	    public void Linq004()
	    {
	        var customers = dataSource.Customers.Where(c => c.Orders.Any())
	            .Select(c => new
	            {
	                CustomerId = c.CustomerID,
	                FirstOrderDate = c.Orders.Min(o => o.OrderDate)
	            });

	        foreach (var c in customers)
	        {
	            ObjectDumper.Write($"CustomerId = {c.CustomerId} " + $"Month = {c.FirstOrderDate.Month} Year = {c.FirstOrderDate.Year}");
	        }
	    }

	    [Category("Task")]
	    [Title("Task 005")]
	    [Description("Сделайте предыдущее задание, но выдайте список отсортированным по году, месяцу, оборотам клиента (от максимального к минимальному) и имени клиента")]
	    public void Linq005()
	    {
	        var customers = dataSource.Customers.Where(c => c.Orders.Any())
	            .Select(c => new
	            {
	                Name = c.CompanyName,
	                FirstOrderDate = c.Orders.Min(o => o.OrderDate),
	                TotalSum = c.Orders.Sum(o => o.Total)
	            }).OrderBy(c => c.FirstOrderDate.Year)
	            .ThenBy(c => c.FirstOrderDate.Month)
	            .ThenByDescending(c => c.TotalSum)
	            .ThenBy(c => c.Name);

	        foreach (var c in customers)
	        {
	            ObjectDumper.Write($"Year = {c.FirstOrderDate.Year} Month = {c.FirstOrderDate.Month} " +
	                               $"TotalSum: {c.TotalSum} Name = {c.Name}");
	        }
	    }

	    [Category("Task")]
	    [Title("Task 006")]
	    [Description("Укажите всех клиентов, у которых указан нецифровой почтовый код или не заполнен регион " +
	                 "или в телефоне не указан код оператора (для простоты считаем, что это равнозначно «нет круглых скобочек в начале»)")]
	    public void Linq006()
	    {
	        var customers = dataSource.Customers.Where(
	            c => c.PostalCode != null && c.PostalCode.All(char.IsDigit)
                     || string.IsNullOrWhiteSpace(c.Region)
	                 || !c.Phone.StartsWith("("));

	        foreach (var c in customers)
	        {
	            ObjectDumper.Write(c);
	        }
	    }

	    [Category("Task")]
	    [Title("Task 007")]
	    [Description("Сгруппируйте все продукты по категориям, внутри – по наличию на складе, " +
	                 "внутри последней группы отсортируйте по стоимости")]
	    public void Linq007()
	    {
	        var categories = dataSource.Products
	            .GroupBy(p => p.Category, (category, products) => new
	            {
	                Category = category,
	                Products = products.GroupBy(item => item.UnitsInStock, (count, prod) => new
	                {
	                    Count = count,
	                    Products = prod.OrderByDescending(p => p.UnitPrice)
	                })
	            });

	        foreach (var category in categories)
	        {
	            ObjectDumper.Write($"Category: {category.Category}");

	            foreach (var products in category.Products)
	            {
	                ObjectDumper.Write($"Products count: {products.Count}");

	                foreach (var product in products.Products)
	                {
	                    ObjectDumper.Write(product);
	                }
	            }

	            ObjectDumper.Write("");
	        }
	    }

	    [Category("Task")]
	    [Title("Task 008")]
	    [Description("Сгруппируйте товары по группам «дешевые», «средняя цена», «дорогие». Границы каждой группы задайте сами")]
	    public void Linq008()
	    {
	        var cheap = 30;
	        var average = 100;

	        var productGroups = dataSource.Products
	            .GroupBy(p => p.UnitPrice <= cheap ? "Cheap"
	                : p.UnitPrice <= average ? "Average price"
                        : "Expensive");

	        foreach (var productGroup in productGroups)
	        {
	            ObjectDumper.Write($"{productGroup.Key}");
	            foreach (var product in productGroup)
	            {
	                ObjectDumper.Write(product);
	            }
	        }
        }

	    [Category("Task")]
	    [Title("Task 009")]
	    [Description("Рассчитайте среднюю прибыльность каждого города (среднюю сумму заказа по всем клиентам из данного города) " +
	                 "и среднюю интенсивность (среднее количество заказов, приходящееся на клиента из каждого города)")]
	    public void Linq009()
	    {
	        var cities = dataSource.Customers
	            .GroupBy(c => c.City, (city, customers) => new
	            {
	                Name = city,
	                AverageIncome = customers.Average(c => c.Orders.Sum(o => o.Total)),
	                AverageOrderCount = customers.Average(c => c.Orders.Length)
	            });

	        foreach (var city in cities)
	        {
	            ObjectDumper.Write(city);
	        }
        }

	    [Category("Task")]
	    [Title("Task 010")]
	    [Description("Сделайте среднегодовую статистику активности клиентов по месяцам (без учета года), " +
	                 "статистику по годам, по годам и месяцам (т.е. когда один месяц в разные годы имеет своё значение)")]
	    public void Linq010()
	    {
	        var orders = dataSource.Customers.SelectMany(n => n.Orders);

	        ObjectDumper.Write("Statistics by month");
	        var monthsGroup = orders.GroupBy(n => n.OrderDate.Month);
	        foreach (var group in monthsGroup.OrderBy(n => n.Key))
	        {
	            ObjectDumper.Write($"{group.Key} : {group.Count()}");
	        }

	        ObjectDumper.Write("Statistics by year");
	        var yearsGroup = orders.GroupBy(n => n.OrderDate.Year);
	        foreach (var group in yearsGroup.OrderBy(n => n.Key))
	        {
	            ObjectDumper.Write($"{group.Key} : {group.Count()}");
	        }

	        ObjectDumper.Write("Statistics by month and year");
	        var monthsYearsGroup = orders.GroupBy(n => new { n.OrderDate.Year, n.OrderDate.Month });
	        foreach (var group in monthsYearsGroup.OrderBy(n => n.Key.Year).ThenBy(n => n.Key.Month))
	        {
	            ObjectDumper.Write($"{group.Key.Year}/{group.Key.Month} : {group.Count()}");
	        }
        }

        [Category("Restriction Operators")]
		[Title("Where - Task 1")]
		[Description("This sample uses the where clause to find all elements of an array with a value less than 5.")]
		public void Linq1()
		{
			int[] numbers = { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };

			var lowNums =
				from num in numbers
				where num < 5
				select num;

			Console.WriteLine("Numbers < 5:");
			foreach (var x in lowNums)
			{
				Console.WriteLine(x);
			}
		}

		[Category("Restriction Operators")]
		[Title("Where - Task 2")]
		[Description("This sample return return all presented in market products")]

		public void Linq2()
		{
			var products =
				from p in dataSource.Products
				where p.UnitsInStock > 0
				select p;

			foreach (var p in products)
			{
				ObjectDumper.Write(p);
			}
		}

	}
}
