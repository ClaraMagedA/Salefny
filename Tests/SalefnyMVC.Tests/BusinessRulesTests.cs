using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;
using SalefnyMVC.Services;
using Xunit;

namespace SalefnyMVC.Tests;

/// <summary>
/// Deterministic unit tests for the pure rules in <see cref="BusinessRules"/>.
/// No database, web host, or clock is involved: every date is fixed explicitly.
/// </summary>
public class BusinessRulesTests
{
    private static DateTime Date(string value) =>
        DateTime.ParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("2026-01-10", "2026-01-12", 3)]
    [InlineData("2026-01-10", "2026-01-10", 1)]
    [InlineData("2026-02-28", "2026-03-02", 3)]
    public void RentalDays_IsInclusiveOfBothEnds(string start, string end, int expected)
        => Assert.Equal(expected, BusinessRules.RentalDays(Date(start), Date(end)));

    [Fact]
    public void RentalDays_ReturnsZeroWhenEndPrecedesStart()
        => Assert.Equal(0, BusinessRules.RentalDays(Date("2026-01-12"), Date("2026-01-10")));

    [Fact]
    public void RentalTotal_ThreeDaysAtTwoHundredIsSixHundred()
        => Assert.Equal(600m, BusinessRules.RentalTotal(3, 200m));

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void RentalTotal_ReturnsZeroForNonPositiveDays(int days)
        => Assert.Equal(0m, BusinessRules.RentalTotal(days, 200m));

    [Fact]
    public void DatesOverlap_ReturnsTrueForOverlappingRanges()
        => Assert.True(BusinessRules.DatesOverlap(
            Date("2026-01-10"), Date("2026-01-15"),
            Date("2026-01-12"), Date("2026-01-20")));

    [Fact]
    public void DatesOverlap_ReturnsTrueForContainedRange()
        => Assert.True(BusinessRules.DatesOverlap(
            Date("2026-01-12"), Date("2026-01-14"),
            Date("2026-01-10"), Date("2026-01-20")));

    [Fact]
    public void DatesOverlap_ReturnsFalseForDisjointRanges()
        => Assert.False(BusinessRules.DatesOverlap(
            Date("2026-01-01"), Date("2026-01-05"),
            Date("2026-01-10"), Date("2026-01-15")));

    [Fact]
    public void DatesOverlap_IsInclusiveWhenNewEndEqualsExistingStart()
        => Assert.True(BusinessRules.DatesOverlap(
            Date("2026-01-01"), Date("2026-01-10"),
            Date("2026-01-10"), Date("2026-01-15")));

    [Fact]
    public void DatesOverlap_IsInclusiveWhenNewStartEqualsExistingEnd()
        => Assert.True(BusinessRules.DatesOverlap(
            Date("2026-01-10"), Date("2026-01-15"),
            Date("2026-01-05"), Date("2026-01-10")));

    [Fact]
    public void DatesOverlap_ReturnsFalseWhenRangesOnlyTouchWithAGap()
        => Assert.False(BusinessRules.DatesOverlap(
            Date("2026-01-01"), Date("2026-01-09"),
            Date("2026-01-10"), Date("2026-01-15")));

    [Fact]
    public void AverageRating_EmptySequenceIsZero()
        => Assert.Equal(0m, BusinessRules.AverageRating(Array.Empty<int>()));

    [Fact]
    public void AverageRating_FourAndFiveIsFourPointFive()
        => Assert.Equal(4.5m, BusinessRules.AverageRating(new[] { 4, 5 }));

    [Fact]
    public void IsWithinAdvanceWindow_AcceptsTodayAndThe180DayBoundary()
    {
        var today = Date("2026-01-01");

        Assert.True(BusinessRules.IsWithinAdvanceWindow(today, today));
        Assert.Equal(Date("2026-06-30"), today.AddDays(BusinessRules.MaxAdvanceDays));
        Assert.True(BusinessRules.IsWithinAdvanceWindow(Date("2026-06-30"), today));
    }

    [Fact]
    public void IsWithinAdvanceWindow_RejectsPastDatesAndDay181()
    {
        var today = Date("2026-01-01");

        Assert.False(BusinessRules.IsWithinAdvanceWindow(today.AddDays(-1), today));
        Assert.False(BusinessRules.IsWithinAdvanceWindow(today.AddDays(181), today));
    }

    [Fact]
    public void CanCancel_ReturnsFalseWhenBookingWasDelivered()
    {
        var booking = NewBooking(BookingStatus.Approved, Date("2026-02-01"), isDelivered: true);

        Assert.False(BusinessRules.CanCancel(booking, Date("2026-01-01")));
    }

    [Fact]
    public void CanCancel_ReturnsFalseWhenBookingAlreadyStarted()
    {
        var booking = NewBooking(BookingStatus.Active, Date("2026-02-01"));

        Assert.False(BusinessRules.CanCancel(booking, Date("2026-01-01")));
    }

    [Fact]
    public void CanCancel_ReturnsFalseWhenStartDateIsToday()
    {
        var today = Date("2026-01-01");
        var booking = NewBooking(BookingStatus.Approved, today);

        Assert.False(BusinessRules.CanCancel(booking, today));
    }

    [Fact]
    public void CanCancel_ReturnsTrueForFutureApprovedBooking()
    {
        var booking = NewBooking(BookingStatus.Approved, Date("2026-02-01"));

        Assert.True(BusinessRules.CanCancel(booking, Date("2026-01-01")));
    }

    [Fact]
    public void CanCancel_ReturnsTrueForFuturePendingBooking()
    {
        var booking = NewBooking(BookingStatus.Pending, Date("2026-02-01"));

        Assert.True(BusinessRules.CanCancel(booking, Date("2026-01-01")));
    }

    private static Booking NewBooking(BookingStatus status, DateTime startDate, bool isDelivered = false)
        => new()
        {
            Status = status,
            StartDate = startDate,
            IsDelivered = isDelivered,
            RenterId = "renter-test"
        };
}
