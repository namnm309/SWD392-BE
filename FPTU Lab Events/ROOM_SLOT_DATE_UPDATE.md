# Room Slot Date Field Update

## ✅ Hoàn thành!

Đã thêm trường **Date** vào RoomSlot để có thể chọn ngày cụ thể cho từng slot.

---

## 📋 Thay đổi chính

### 1. **Entity RoomSlot** - Thêm trường `Date`
```csharp
public class RoomSlot : BaseEntity
{
    public Guid RoomId { get; set; }
    public DateTime Date { get; set; }          // ← MỚI: Ngày cụ thể (2025-10-20)
    public int SlotNumber { get; set; }         // 1-8
    public int DayOfWeek { get; set; }          // Auto-calculated from Date
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public Guid? EventId { get; set; }
    public string? Status { get; set; }
}
```

### 2. **Unique Constraint** - Thay đổi từ `(RoomId, DayOfWeek, SlotNumber)` → `(RoomId, Date, SlotNumber)`
- Trước: Mỗi room chỉ có 1 slot cho mỗi **DayOfWeek + SlotNumber**
- Bây giờ: Mỗi room chỉ có 1 slot cho mỗi **Date + SlotNumber**

### 3. **DTO Updates**
```csharp
public class RoomSlotInfo
{
    public DateTime Date { get; set; }          // ← MỚI
    public string DateFormatted { get; set; }   // ← MỚI: "20/10/2025"
    public int SlotNumber { get; set; }
    public int DayOfWeek { get; set; }
    public string DayOfWeekName { get; set; }
    // ... other fields
}

public class CreateRoomSlotRequest
{
    public Guid RoomId { get; set; }
    public DateTime Date { get; set; }          // ← MỚI (bắt buộc)
    public int SlotNumber { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    // ... other fields
}
```

---

## 🔌 Cách sử dụng API

### ✅ Tạo Room Slot mới (với Date)

**Request:**
```http
POST /api/rooms/slots
Authorization: Bearer {token}

{
  "roomId": "7784fc27-811c-4efa-b585-9a05a2eb1ba0",
  "date": "2025-10-21",                    ← Bắt buộc: chọn ngày cụ thể
  "slotNumber": 3,
  "startTime": "12:30:00",
  "endTime": "14:45:00",
  "eventId": "f087e897-06b4-4c7d-9a57-243fc4b54cdf",
  "status": "attended"
}
```

**Response:**
```json
{
  "data": {
    "id": "guid",
    "date": "2025-10-21T00:00:00Z",
    "dateFormatted": "21/10/2025",
    "slotNumber": 3,
    "dayOfWeek": 2,
    "dayOfWeekName": "Tuesday",
    "startTime": "12:30:00",
    "endTime": "14:45:00",
    "timeRange": "12:30-14:45",
    "eventId": "f087e897-06b4-4c7d-9a57-243fc4b54cdf",
    "eventTitle": "SWD392",
    "status": "attended"
  },
  "code": 201,
  "message": "Created"
}
```

---

### ✅ Generate Weekly Slots (Tự động tạo cho 1 tuần)

**Request:**
```http
POST /api/rooms/{roomId}/slots/generate-weekly?weekStartDate=2025-10-20
Authorization: Bearer {token}
```

**Chức năng:**
- Tạo slots cho 7 ngày bắt đầu từ `weekStartDate`
- Chỉ tạo cho weekdays (Monday-Friday)
- Mỗi ngày tạo 8 slots (Slot 1-8)
- Tổng: 40 slots (5 ngày x 8 slots)
- Không tạo trùng nếu đã tồn tại

---

### ✅ Lấy Slots theo Date Range

**Request:**
```http
GET /api/rooms/{roomId}/slots/date-range?startDate=2025-10-20&endDate=2025-10-26
Authorization: Bearer {token}
```

**Response:**
```json
{
  "data": [
    {
      "id": "guid",
      "date": "2025-10-20T00:00:00Z",
      "dateFormatted": "20/10/2025",
      "slotNumber": 1,
      "dayOfWeek": 1,
      "dayOfWeekName": "Monday",
      "timeRange": "07:00-09:00",
      "eventId": null,
      "status": null
    },
    {
      "id": "guid",
      "date": "2025-10-20T00:00:00Z",
      "dateFormatted": "20/10/2025",
      "slotNumber": 2,
      "dayOfWeek": 1,
      "dayOfWeekName": "Monday",
      "timeRange": "09:15-11:15",
      "eventId": "guid",
      "eventTitle": "SWD392",
      "status": "attended"
    }
    // ... more slots
  ]
}
```

---

## 🎯 Use Cases

### 1. **Tạo lịch cho một ngày cụ thể**

```javascript
// Tạo Slot 3 cho ngày 21/10/2025
POST /api/rooms/slots
Body: {
  "roomId": "room-guid",
  "date": "2025-10-21",      // Chọn ngày cụ thể
  "slotNumber": 3,
  "startTime": "12:30:00",
  "endTime": "14:45:00"
}
```

### 2. **Tạo lịch cho cả tuần (Monday-Friday)**

```javascript
// Tự động tạo 40 slots từ 20/10 đến 24/10 (Monday-Friday)
POST /api/rooms/{roomId}/slots/generate-weekly?weekStartDate=2025-10-20
```

### 3. **Xem lịch theo tuần**

```javascript
// Lấy tất cả slots từ 20/10 đến 26/10
GET /api/rooms/{roomId}/slots/date-range?startDate=2025-10-20&endDate=2025-10-26
```

---

## 🔄 Migration Applied

✅ Migration: `20251026172359_AddDateToRoomSlot`
✅ Database đã được update thành công

---

## ✨ Lợi ích của thay đổi

### Trước (chỉ có DayOfWeek):
- ❌ Không thể tạo slot cho ngày cụ thể
- ❌ Không biết slot thuộc tuần nào
- ❌ Khó quản lý lịch theo ngày

### Sau (có Date):
- ✅ Chọn ngày chính xác: "20/10/2025", "21/10/2025", ...
- ✅ Dễ dàng quản lý lịch theo tuần/tháng
- ✅ Có thể tạo nhiều slot cho cùng DayOfWeek (ví dụ: Monday tuần 1, Monday tuần 2)
- ✅ Phù hợp với lịch học thực tế

---

## 📝 Validation

Khi tạo RoomSlot, hệ thống sẽ validate:

1. ✅ **Date** là bắt buộc
2. ✅ **Room** phải tồn tại
3. ✅ **Event** (nếu có) phải tồn tại
4. ✅ **SlotNumber** phải từ 1-8
5. ✅ **Không trùng**: Mỗi (RoomId, Date, SlotNumber) phải unique

---

## 🧪 Testing

### Test Case 1: Tạo slot thành công
```json
POST /api/rooms/slots
{
  "roomId": "7784fc27-811c-4efa-b585-9a05a2eb1ba0",
  "date": "2025-10-21",
  "slotNumber": 3,
  "startTime": "12:30:00",
  "endTime": "14:45:00"
}
✅ Expected: 201 Created
```

### Test Case 2: Tạo slot trùng (cùng room, date, slot)
```json
POST /api/rooms/slots (call lần 2 với cùng data)
❌ Expected: 400 Bad Request
Error: "Room slot already exists for Room XXX, Date 21/10/2025, Slot 3"
```

### Test Case 3: Generate weekly slots
```http
POST /api/rooms/{roomId}/slots/generate-weekly?weekStartDate=2025-10-20
✅ Expected: 201 Created với 40 slots (5 days x 8 slots)
```

---

## 🎨 Frontend Integration

```typescript
// Tạo slot cho ngày cụ thể
const createSlot = async (roomId: string, date: Date, slotNumber: number) => {
  const response = await fetch('/api/rooms/slots', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      roomId: roomId,
      date: date.toISOString().split('T')[0], // "2025-10-21"
      slotNumber: slotNumber,
      startTime: "12:30:00",
      endTime: "14:45:00"
    })
  });
  return await response.json();
};

// Lấy lịch tuần
const getWeeklySchedule = async (roomId: string, startDate: Date) => {
  const endDate = new Date(startDate);
  endDate.setDate(endDate.getDate() + 6); // +6 days
  
  const response = await fetch(
    `/api/rooms/${roomId}/slots/date-range?` +
    `startDate=${startDate.toISOString().split('T')[0]}&` +
    `endDate=${endDate.toISOString().split('T')[0]}`,
    { headers: { Authorization: `Bearer ${token}` } }
  );
  return await response.json();
};

// Generate slots cho tuần
const generateWeekSlots = async (roomId: string, weekStart: Date) => {
  const response = await fetch(
    `/api/rooms/${roomId}/slots/generate-weekly?` +
    `weekStartDate=${weekStart.toISOString().split('T')[0]}`,
    {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` }
    }
  );
  return await response.json();
};
```

---

**Updated:** October 26, 2025  
**Status:** ✅ Ready to use  
**Migration:** Applied successfully

