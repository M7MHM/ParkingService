namespace ParkingBooking.Domain.Enums;

/// <summary>
/// حالة الحجز
/// </summary>
public enum BookingStatus
{
    Pending = 0,    // اتعمل بس لسه مأكدش (مستني دفع مثلاً)
    Confirmed = 1,  // اتأكد (الدفع تم / الموقف اتحجز فعليًا)
    Cancelled = 2,  // اتلغى
    Completed = 3,  // خلص عادي (السيارة خرجت)
    Expired = 4     // فضل Pending من غير تأكيد لحد ما وقته خلص
}
