# Event với Room & RoomSlot Selection Guide

## ✅ Hoàn thành!

Đã thêm tính năng chọn **Room** và **RoomSlots** khi tạo Event.

---

## 🎯 Tính năng mới

Khi tạo Event, bạn có thể:
1. ✅ **Chọn Room** (phòng học)
2. ✅ **Chọn RoomSlots** (các tiết học cụ thể trong phòng)
3. ✅ **Auto-assign Event vào RoomSlots** được chọn
4. ✅ **Validation**: Slot không được trùng event, các slots phải cùng 1 room

---

## 📋 Cấu trúc DTOs

### CreateEventRequest (Updated)
```json
{
  "title": "SWD392 - Software Architecture",
  "description": "Học kiến trúc phần mềm",
  "startDate": "2025-10-20T07:00:00Z",
  "endDate": "2025-12-20T17:00:00Z",
  "location": "NVH Building",
  "status": 0,  // 0=Active
  "visibility": true,
  "recurrenceRule": null,
  
  // ← MỚI: Chọn Room và RoomSlots
  "roomId": "7784fc27-811c-4efa-b585-9a05a2eb1ba0",  // Optional
  "roomSlotIds": [  // Optional: Danh sách slot IDs
    "slot-id-1",
    "slot-id-2",
    "slot-id-3"
  ]
}
```

### EventDetail (Response - Updated)
```json
{
  "id": "event-guid",
  "title": "SWD392 - Software Architecture",
  "description": "Học kiến trúc phần mềm",
  "startDate": "2025-10-20T07:00:00Z",
  "endDate": "2025-12-20T17:00:00Z",
  
  // ← MỚI: Thông tin Room và RoomSlots
  "roomId": "room-guid",
  "roomName": "NVH.404",
  "roomSlots": [
    {
      "id": "slot-guid-1",
      "roomId": "room-guid",
      "roomName": "NVH.404",
      "date": "2025-10-21T00:00:00Z",
      "dateFormatted": "21/10/2025",
      "slotNumber": 3,
      "dayOfWeekName": "Tuesday",
      "timeRange": "12:30-14:45",
      "status": "attended"
    },
    {
      "id": "slot-guid-2",
      "roomId": "room-guid",
      "roomName": "NVH.404",
      "date": "2025-10-22T00:00:00Z",
      "dateFormatted": "22/10/2025",
      "slotNumber": 3,
      "dayOfWeekName": "Wednesday",
      "timeRange": "12:30-14:45",
      "status": null
    }
  ],
  
  "bookings": [],
  "isUpcoming": true,
  ...
}
```

---

## 🔌 API Endpoints

### 1. Lấy danh sách Rooms
```http
GET /api/rooms
Authorization: Bearer {token}
```

**Response:**
```json
{
  "data": [
    {
      "id": "7784fc27-811c-4efa-b585-9a05a2eb1ba0",
      "name": "NVH.404",
      "description": "Phòng thực hành",
      "location": "NVH Building",
      "capacity": 50,
      "status": "Available"
    }
  ]
}
```

---

### 2. Lấy Available RoomSlots của một Room (← MỚI)
```http
GET /api/rooms/{roomId}/slots/available
GET /api/rooms/{roomId}/slots/available?startDate=2025-10-20&endDate=2025-10-26
Authorization: Bearer {token}
```

**Chức năng:**
- Lấy danh sách RoomSlots **chưa có Event** (available)
- Optional: Filter theo date range

**Response:**
```json
{
  "data": [
    {
      "id": "slot-guid-1",
      "date": "2025-10-21T00:00:00Z",
      "dateFormatted": "21/10/2025",
      "slotNumber": 1,
      "dayOfWeek": 2,
      "dayOfWeekName": "Tuesday",
      "startTime": "07:00:00",
      "endTime": "09:00:00",
      "timeRange": "07:00-09:00",
      "eventId": null,      ← Chưa có event
      "eventTitle": null,
      "status": null
    },
    {
      "id": "slot-guid-2",
      "date": "2025-10-21T00:00:00Z",
      "dateFormatted": "21/10/2025",
      "slotNumber": 3,
      "dayOfWeek": 2,
      "dayOfWeekName": "Tuesday",
      "startTime": "12:30:00",
      "endTime": "14:45:00",
      "timeRange": "12:30-14:45",
      "eventId": null,
      "eventTitle": null,
      "status": null
    }
  ]
}
```

---

### 3. Tạo Event với Room & RoomSlots (← UPDATED)
```http
POST /api/events
Authorization: Bearer {token}
Role: Admin

Body:
{
  "title": "SWD392 - Software Architecture",
  "description": "Học kiến trúc phần mềm",
  "startDate": "2025-10-20T07:00:00Z",
  "endDate": "2025-12-20T17:00:00Z",
  "location": "NVH Building",
  "status": 0,
  "visibility": true,
  "roomId": "7784fc27-811c-4efa-b585-9a05a2eb1ba0",
  "roomSlotIds": [
    "slot-id-1",
    "slot-id-2",
    "slot-id-3"
  ]
}
```

**Validation:**
1. ✅ Room phải tồn tại
2. ✅ RoomSlots phải tồn tại
3. ✅ Tất cả RoomSlots phải thuộc cùng 1 Room
4. ✅ RoomId (nếu có) phải match với Room của các Slots
5. ✅ RoomSlots chưa được assign cho Event khác

**Response:**
```json
{
  "data": {
    "id": "new-event-guid",
    "title": "SWD392 - Software Architecture",
    "roomId": "room-guid",
    "roomName": "NVH.404",
    "roomSlots": [
      {
        "id": "slot-guid-1",
        "roomName": "NVH.404",
        "date": "2025-10-21T00:00:00Z",
        "dateFormatted": "21/10/2025",
        "slotNumber": 3,
        "timeRange": "12:30-14:45"
      }
    ],
    ...
  },
  "code": 201,
  "message": "Created"
}
```

---

### 4. Lấy thông tin Event (GET /api/events/{id})
Response bây giờ sẽ có thêm `roomId`, `roomName`, và `roomSlots` array.

---

## 🎯 Use Cases

### Use Case 1: Tạo Event cho môn học SWD392

**Bước 1: Lấy danh sách rooms**
```http
GET /api/rooms
```

**Bước 2: Chọn room NVH.404, lấy available slots**
```http
GET /api/rooms/7784fc27-811c-4efa-b585-9a05a2eb1ba0/slots/available?startDate=2025-10-20&endDate=2025-12-20
```

**Bước 3: Chọn các slots (ví dụ: Tuesday Slot 3, Thursday Slot 3) và tạo event**
```http
POST /api/events
Body: {
  "title": "SWD392",
  "startDate": "2025-10-20",
  "endDate": "2025-12-20",
  "roomId": "room-guid",
  "roomSlotIds": ["tuesday-slot-3-id", "thursday-slot-3-id"]
}
```

**Kết quả:**
- Event được tạo
- 2 RoomSlots được gán `eventId` = event vừa tạo
- Các slots này không còn available nữa

---

### Use Case 2: Xem lịch của Event

**Request:**
```http
GET /api/events/{eventId}
```

**Response sẽ có thông tin:**
- Room nào
- Các slots nào (ngày, giờ)
- Status của từng slot (attended, absent, ...)

---

## 🔄 Workflow Frontend

```typescript
// Step 1: Get all rooms
const rooms = await fetch('/api/rooms');

// Step 2: User chọn room → Get available slots
const selectedRoomId = "room-guid";
const availableSlots = await fetch(
  `/api/rooms/${selectedRoomId}/slots/available?` +
  `startDate=2025-10-20&endDate=2025-12-20`
);

// Step 3: User chọn slots → Create event
const selectedSlotIds = ["slot-1", "slot-2", "slot-3"];
const newEvent = await fetch('/api/events', {
  method: 'POST',
  body: JSON.stringify({
    title: "SWD392",
    ...otherFields,
    roomId: selectedRoomId,
    roomSlotIds: selectedSlotIds
  })
});

// Step 4: Show success với thông tin room và slots
console.log(`Event created in room: ${newEvent.data.roomName}`);
console.log(`Slots: ${newEvent.data.roomSlots.length}`);
```

---

## ✅ Validation Rules

### Khi tạo Event với RoomSlots:

1. **Room Validation:**
   - ❌ Error nếu `roomId` không tồn tại
   - ✅ OK nếu không cung cấp `roomId` (optional)

2. **RoomSlots Validation:**
   - ❌ Error nếu một trong các `roomSlotIds` không tồn tại
   - ❌ Error nếu các slots thuộc nhiều room khác nhau
   - ❌ Error nếu `roomId` được cung cấp nhưng không match với room của slots
   - ❌ Error nếu slot đã có `eventId` (đã được assign)
   - ✅ OK nếu không cung cấp `roomSlotIds` (optional)

3. **Combined:**
   - ✅ Có thể tạo Event không cần Room/Slots
   - ✅ Có thể tạo Event chỉ với RoomId (không có slots)
   - ✅ Có thể tạo Event với Slots (RoomId auto-detect từ slots)
   - ✅ Có thể tạo Event với cả RoomId và Slots (phải match)

---

## 🧪 Testing Examples

### Test 1: Tạo Event thành công với RoomSlots
```json
POST /api/events
{
  "title": "SWD392",
  "startDate": "2025-10-20",
  "endDate": "2025-12-20",
  "roomSlotIds": ["available-slot-1", "available-slot-2"]
}
✅ Expected: 201 Created, slots được gán eventId
```

### Test 2: Tạo Event với slot đã có event
```json
POST /api/events
{
  "title": "PRN222",
  "roomSlotIds": ["slot-already-has-event"]
}
❌ Expected: 400 Bad Request
Error: "RoomSlot (Date: 21/10/2025, Slot: 3) is already assigned to another event"
```

### Test 3: Tạo Event với slots thuộc nhiều rooms
```json
POST /api/events
{
  "title": "Test",
  "roomSlotIds": ["room-A-slot", "room-B-slot"]
}
❌ Expected: 400 Bad Request
Error: "All RoomSlots must belong to the same Room"
```

### Test 4: Get available slots
```http
GET /api/rooms/{roomId}/slots/available
✅ Expected: Chỉ trả về slots chưa có eventId
```

---

## 📊 Database Changes

### RoomSlot Table
```
┌─────────┬──────────┬────────┬─────────────────┬───────────────┐
│ SlotNumber │ Date     │ RoomId │ EventId         │ Status        │
├─────────┼──────────┼────────┼─────────────────┼───────────────┤
│ 3       │ 21/10/25 │ NVH404 │ null            │ null          │ ← Available
│ 3       │ 22/10/25 │ NVH404 │ swd392-event-id │ "attended"    │ ← Assigned
│ 3       │ 23/10/25 │ NVH404 │ swd392-event-id │ "absent"      │ ← Assigned
└─────────┴──────────┴────────┴─────────────────┴───────────────┘
```

---

## 🎨 UI Flow Suggestion

### Tạo Event Form:
```
┌──────────────────────────────────────────┐
│ Create Event                             │
├──────────────────────────────────────────┤
│ Title: [SWD392 - Software Architecture ] │
│ Description: [...                       ]│
│ Start Date: [2025-10-20]                 │
│ End Date:   [2025-12-20]                 │
│                                          │
│ ┌────────────────────────────────────┐  │
│ │ Select Room (Optional)             │  │
│ ├────────────────────────────────────┤  │
│ │ ○ NVH.404 (50 seats)              │  │
│ │ ○ NVH.502 (40 seats)              │  │
│ │ ○ DE.301 (35 seats)               │  │
│ └────────────────────────────────────┘  │
│                                          │
│ ┌────────────────────────────────────┐  │
│ │ Select Time Slots (Optional)       │  │
│ ├────────────────────────────────────┤  │
│ │ Room: NVH.404                      │  │
│ │ Date Range: 20/10 - 26/10          │  │
│ │                                    │  │
│ │ ☑ Mon, 21/10 - Slot 3 (12:30-14:45)│  │
│ │ ☑ Wed, 23/10 - Slot 3 (12:30-14:45)│  │
│ │ ☐ Fri, 25/10 - Slot 3 (12:30-14:45)│  │ ← Already taken
│ └────────────────────────────────────┘  │
│                                          │
│ [Create Event]  [Cancel]                 │
└──────────────────────────────────────────┘
```

---

## 🚀 Để sử dụng:

**1. Restart server** (nếu đang chạy)
```bash
# Tắt server hiện tại (Ctrl+C)
cd "ControllerLayer"
dotnet run
```

**2. Test với Swagger:**
```
http://localhost:7241/swagger/index.html
```

**3. Test flow:**
1. GET /api/rooms → Lấy room
2. POST /api/rooms/{roomId}/slots/generate-weekly → Tạo slots cho room
3. GET /api/rooms/{roomId}/slots/available → Xem slots có sẵn
4. POST /api/events (với roomSlotIds) → Tạo event và gán slots

---

## 📝 Notes

- `roomId` và `roomSlotIds` đều **optional**
- Có thể tạo Event không cần Room/Slots (như trước)
- Có thể tạo Event chỉ với Room hoặc chỉ với Slots
- Khi gán Slots vào Event, field `EventId` trong RoomSlot sẽ được update
- Khi xóa Event, các RoomSlots sẽ tự động set `EventId = null` (OnDelete.SetNull)

---

**Updated:** October 30, 2025  
**Status:** ✅ Ready to use

