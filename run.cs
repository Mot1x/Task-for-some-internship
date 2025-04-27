using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

class HotelCapacity
{
    private static readonly Regex NameRegex = new Regex("\"name\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex CheckInRegex = new Regex("\"check-in\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);

    private static readonly Regex
        CheckOutRegex = new Regex("\"check-out\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);

    static int ConvertStringDateToInt(string date)
    {
        var mult = (int)10e6;
        var res = 0;

        for (var i = 0; i < date.Length; i++)
        {
            if (i == 4 || i == 7)
                continue;
            res += (date[i] - '0') * mult;
            mult /= 10;
        }

        return res;
    }

    static bool CheckCapacity(int maxCapacity, List<Guest> guests)
    {
        var changesInDate = new Dictionary<int, int>();
        foreach (var guest in guests)
        {
            var checkIn = ConvertStringDateToInt(guest.CheckIn);
            var checkOut = ConvertStringDateToInt(guest.CheckOut);
            if (!changesInDate.ContainsKey(checkIn))
                changesInDate[checkIn] = 0;
            if (!changesInDate.ContainsKey(checkOut))
                changesInDate[checkOut] = 0;
            changesInDate[checkIn]++;
            changesInDate[checkOut]--;
        }

        var sortedDates = changesInDate.Keys.ToArray();
        Array.Sort(sortedDates);

        var current = 0;
        foreach (var date in sortedDates)
        {
            current += changesInDate[date];
            if (current > maxCapacity)
                return false;
        }

        return true;
    }


    class Guest
    {
        public string Name { get; set; }
        public string CheckIn { get; set; }
        public string CheckOut { get; set; }
    }


    static void Main()
    {
        var maxCapacity = int.Parse(Console.ReadLine());
        var maybeN = Console.ReadLine();
        var n = maybeN.Length == 0
            ? int.Parse(Console.ReadLine())
            : int.Parse(maybeN);

        var guests = new List<Guest>();
        for (var i = 0; i < n; i++)
        {
            var line = Console.ReadLine();
            var guest = ParseGuest(line);
            guests.Add(guest);
        }

        var result = CheckCapacity(maxCapacity, guests);
        Console.WriteLine(result);
    }

    static Guest ParseGuest(string json)
    {
        var guest = new Guest();

        var nameMatch = NameRegex.Match(json);
        if (nameMatch.Success)
            guest.Name = nameMatch.Groups[1].Value;

        var checkInMatch = CheckInRegex.Match(json);
        if (checkInMatch.Success)
            guest.CheckIn = checkInMatch.Groups[1].Value;

        var checkOutMatch = CheckOutRegex.Match(json);
        if (checkOutMatch.Success)
            guest.CheckOut = checkOutMatch.Groups[1].Value;

        return guest;
    }
}