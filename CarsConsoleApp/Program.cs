using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        try
        {
            var parts = ReadPartsFromFile("../../Data/alkatreszek.csv");
            var cars = ReadCarsFromFile("../../Data/autok.csv");
            var inventory = ReadInventoryFromFile("../../Data/keszlet.csv", cars);

            var carsCost = new List<CarsCost>();

            carsCost = CalculateCarValues(parts, cars, inventory);
            if (carsCost.Any())
            {
                var totalCost = 0.0;
                foreach (var car in carsCost)
                {
                    totalCost += car.Price;
                }
                Console.WriteLine($"The total value of the inventory is: {totalCost:F2} gold");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file or wrong data in one of the files: {ex.Message}");
        }
        Console.ReadKey();
    }
    private static List<Part> ReadPartsFromFile(string filePath)
    {
        var parts = new List<Part>();
        using (var reader = new StreamReader(filePath))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                validatePartsFileValues(values, filePath);
                var part = createPart(values, filePath);
                parts.Add(part);
            }
        }

        validateSubPartsValues(parts, filePath);

        return parts;
    }
    private static List<Car> ReadCarsFromFile(string filePath)
    {
        var cars = new List<Car>();
        using (var reader = new StreamReader(filePath))
        {
            Car car = null;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                validateCarsFileValues(line, car, filePath);

                if (!string.IsNullOrWhiteSpace(values[0]))
                {
                    if (car != null) cars.Add(car);
                    car = new Car
                    {
                        Type = values[0].Trim(),
                        AssemblyCost = double.Parse(values[2].Trim()),
                        PartRequirements = new List<PartRequirement>()
                    };
                }
                else if (car != null && !string.IsNullOrWhiteSpace(values[1]) && int.TryParse(values[2], out int r3) && int.Parse(values[2]) > 0)
                {
                    car.PartRequirements.Add(new PartRequirement
                    {
                        Name = values[1].Trim(),
                        Quantity = int.Parse(values[2].Trim())
                    });
                }
            }
            if (car != null) cars.Add(car);
        }
        return cars;
    }
    private static List<InventoryItem> ReadInventoryFromFile(string filePath, List<Car> cars)
    {
        var inventory = new List<InventoryItem>();

        using (var reader = new StreamReader(filePath))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                validateInventoryFileValues(values, filePath, cars);

                var inventoryItem = new InventoryItem
                {
                    CarType = values[0],
                    CompletionPercentage = double.Parse(values[1])
                };

                inventory.Add(inventoryItem);
            }
        }
        return inventory;
    }
    private static void validatePartsFileValues(string[] values, string filePath)
    {
        if (string.IsNullOrWhiteSpace(values[0]))
            throw new Exception($"In file: '{filePath}' A part must have a name!");

        if (!string.IsNullOrWhiteSpace(values[1]) && !isNumberBetweenZeroAndHundred(values[1]))
            throw new Exception($"In file: '{filePath}' The second cell if not empty, should be a number between 0-100!");

        if (!string.IsNullOrWhiteSpace(values[2]) && !isPositiveNumber(values[2]))
            throw new Exception($"In file: '{filePath}' The third cell if not empty, should be a number bigger than 0!");
    }
    private static void validateSubPartsValues(List<Part> parts, string filePath)
    {
        foreach(var part in parts)
        {
            foreach(var subPart in part.SubParts)
            {
                var validPart = parts.FirstOrDefault(p => p.Name == subPart.Name);
                if (validPart == null)
                    throw new Exception($"In file: '{filePath}' A given sub part does not exist in the list of parts!");
            }
        }
    }
    private static void validateCarsFileValues(string line, Car car, string filePath)
    {
        var values = line.Split(',');
        if (values.Length != 3)
            throw new Exception($"In file: '{filePath}' All rows have to contain 3 elements!");
        if (line == ",," && (car == null || car.PartRequirements.Count == 0))
            throw new Exception($"In file: '{filePath}' There is an empty line in the wrong place!");
        if (!string.IsNullOrWhiteSpace(values[0]))
        {
            if (!string.IsNullOrWhiteSpace(values[1]) || !(isPositiveNumber(values[2])))
                throw new Exception($"In file: '{filePath}' The first elem of the row is the type then the second elem must be empty and the third must contain a positive number!");
            if (car != null && car.PartRequirements.Count == 0)
                throw new Exception($"In file: '{filePath}' A car type does not have any required parts!");
        }
        else
        {
            if (car == null)
                throw new Exception($"In file: '{filePath}' There should be a car type in the first cell!");
            if (line != ",," && (string.IsNullOrWhiteSpace(values[1]) || !isPositiveNumber(values[2])))
                throw new Exception($"In file: '{filePath}' If the first elem is empty in a row and it is not an empty row then the second elem can not be empty and the third elem is a positive number");
        }
    }
    private static void validateInventoryFileValues(string[] values, string filePath, List<Car> cars)
    {
        if (values.Length != 2)
            throw new Exception($"In file: '{filePath}' The required number of cells in a row is 2!");
        if (string.IsNullOrWhiteSpace(values[0]) || !isPositiveNumber(values[1]))
            throw new Exception($"In file: '{filePath}' The first cell has to be a string and the second has to be a number!");
        if (!isNumberBetweenZeroAndHundred(values[1]))
            throw new Exception($"In file: '{filePath}' The percentage of the completion must be between 0 and 100!");
        // If there is a type in the keszlet.csv which is not in the autok.csv then it throws an exception that is why we need the list of the cars here
        var car = cars.FirstOrDefault(c => c.Type == values[0]);
        if (car == null)
            throw new Exception($"In file: '{filePath}' One or more of the given car types does not exist in the autok.csv!");
    }
    private static Part createPart(string[] values, string filePath)
    {
        var part = new Part
        {
            Name = values[0],
            AssemblyPercentage = values.Length > 1 && values[1] != "" ? double.Parse(values[1]) : 0.0,
            Price = values.Length > 2 && values[2] != "" ? double.Parse(values[2]) : 0.0,
            SubParts = new List<SubPart>()
        };

        if (values.Length > 3 && !string.IsNullOrWhiteSpace(values[3]))
        {
            for (int i = 3; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    var subPart = createSubPart(values[i], filePath);
                    part.SubParts.Add(subPart);
                }
            }
        }

        if (part.SubParts.Count() == 0 && part.Price == 0)
            throw new Exception($"In file: '{filePath}' A part must have subparts or a price!");

        return part;
    }
    //If a part is built of sub parts then createSubPart creates and returns it in as a SubPart class item
    private static SubPart createSubPart(string value, string filePath)
    {
        var subPart = new SubPart();
        var subPartValues = value.Split(' ');
        if (int.TryParse(subPartValues[0], out int number))
        {
            if (number <= 0)
                throw new Exception($"In file: '{filePath}' Quantity of subpart has to be bigger than 0!");
            subPart.Quantity = int.Parse(subPartValues[0]);
            for (int j = 1; j < subPartValues.Length; j++)
            {
                subPart.Name += $"{subPartValues[j]} ";
            }
            subPart.Name = subPart.Name.Trim();
            if (string.IsNullOrWhiteSpace(subPart.Name))
                throw new Exception($"In file: '{filePath}' A subpart must have a name!");
        }
        else
            throw new Exception($"In file: '{filePath}' If a part has subparts then the cell must start with the quantity");
        return subPart;
    }
    private static List<CarsCost> CalculateCarValues(List<Part> parts, List<Car> cars, List<InventoryItem> inventory)
    {
        var carsCosts = new List<CarsCost>();
        foreach (var elem in inventory)
        {
            var price = 0.0;

            var car = cars.FirstOrDefault(c => c.Type == elem.CarType);

            if (car != null)
            {
                price += car.AssemblyCost * (elem.CompletionPercentage / 100);

                foreach (var partRequirement in car.PartRequirements)
                {
                    var part = parts.FirstOrDefault(p => p.Name == partRequirement.Name);
                    if (part.AssemblyPercentage <= elem.CompletionPercentage)
                    {
                        price += CalculatePartValues(part, parts) * partRequirement.Quantity;
                    }
                }
            }
            Console.WriteLine($"{elem.CarType}: {elem.CompletionPercentage}%, {price:F2} gold");
            //To calculate the total inventory value it is easier to return the costs for each car
            var carCost = new CarsCost
            {
                Type = elem.CarType,
                Price = price,
            };
            carsCosts.Add(carCost);
        }
        return carsCosts;
    }
    // CalculatePartValues gives back the price of a part which contains the price of the sub parts if there is any 
    private static double CalculatePartValues(Part part, List<Part> parts)
    {
        if (part.Price > 0)
        {
            return (double)part.Price;
        }
        var price = 0.0;
        foreach (var subPart in part.SubParts)
        {
            price += CalculateSubPartValues(subPart, parts);
        }
        return price;
    }
    // CalculateSubPartValues is a recursive function because a sub part can also have sub parts and so on
    private static double CalculateSubPartValues(SubPart subPart, List<Part> parts)
    {
        var price = 0.0;
        var part = parts.FirstOrDefault(p => p.Name == subPart.Name);
        if (part.Price > 0)
        {
            return (double)part.Price * subPart.Quantity;
        }
        foreach (var sPart in part.SubParts)
        {
            price += CalculateSubPartValues(sPart, parts);
        }
        return price;
    }
    private static bool isPositiveNumber(string value)
    {
        if ((double.TryParse(value, out double r)) && r >= 0) return true;
        else return false;
    }
    private static bool isNumberBetweenZeroAndHundred(string value)
    {
        if(double.TryParse(value, out double r) && r >=0 && r <= 100) return true;
        else return false;
    }

}
