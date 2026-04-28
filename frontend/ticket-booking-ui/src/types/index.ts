export interface AuthResponse {
  token: string;
  refreshToken: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  expiresAt: string;
}

export interface Event {
  id: string;
  name: string;
  description: string;
  category: string;
  venue: string;
  city: string;
  eventDate: string;
  totalSeats: number;
  availableSeats: number;
  ticketPrice: number;
  imageUrl?: string;
  isActive: boolean;
}

export interface Seat {
  id: string;
  seatNumber: string;
  row: string;
  section: string;
  isBooked: boolean;
}

export interface Booking {
  id: string;
  eventId: string;
  eventName: string;
  eventDate: string;
  seatNumber: string;
  amount: number;
  status: string;
  createdAt: string;
}

export interface Payment {
  id: string;
  bookingId: string;
  amount: number;
  status: string;
  transactionReference: string;
  createdAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}
