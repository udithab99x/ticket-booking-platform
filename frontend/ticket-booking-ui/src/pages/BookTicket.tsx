import { useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import api from '../lib/api';
import type { ApiResponse, Booking, Payment, Event, Seat } from '../types';

export function BookTicket() {
  const { id: eventId } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { state } = useLocation() as { state: { seat: Seat; event: Event } };
  const { seat, event } = state ?? {};

  const [cardHolder, setCardHolder] = useState('');
  const [cardLast4, setCardLast4] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  if (!seat || !event) {
    navigate(`/events/${eventId}`);
    return null;
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (cardLast4.length !== 4 || !/^\d{4}$/.test(cardLast4)) {
      setError('Please enter the last 4 digits of your card.');
      return;
    }
    setLoading(true);
    setError('');

    try {
      // 1. Create booking
      const bookingRes = await api.post<ApiResponse<Booking>>('/api/bookings', {
        eventId: event.id,
        seatId: seat.id,
        seatNumber: seat.seatNumber,
      });
      if (!bookingRes.data.success) throw new Error(bookingRes.data.message || 'Booking failed');
      const booking = bookingRes.data.data;

      // 2. Process payment
      const paymentRes = await api.post<ApiResponse<Payment>>('/api/payments', {
        bookingId: booking.id,
        amount: event.ticketPrice,
        cardLastFour: cardLast4,
        cardHolderName: cardHolder,
      });
      if (!paymentRes.data.success) throw new Error(paymentRes.data.message || 'Payment failed');

      navigate('/my-bookings', { state: { success: true } });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Something went wrong';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-lg mx-auto px-6 py-12">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Complete Your Booking</h1>

      {/* Order Summary */}
      <div className="bg-indigo-50 rounded-xl p-5 mb-6 border border-indigo-200">
        <h2 className="font-semibold text-indigo-800 mb-3">Order Summary</h2>
        <div className="text-sm text-gray-700 space-y-1">
          <p><span className="font-medium">Event:</span> {event.name}</p>
          <p><span className="font-medium">Venue:</span> {event.venue}, {event.city}</p>
          <p><span className="font-medium">Seat:</span> {seat.seatNumber}</p>
          <div className="border-t border-indigo-200 pt-2 mt-2 flex justify-between font-bold text-indigo-900">
            <span>Total</span>
            <span>${event.ticketPrice}</span>
          </div>
        </div>
      </div>

      {/* Payment Form */}
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Cardholder Name</label>
          <input
            type="text"
            required
            value={cardHolder}
            onChange={e => setCardHolder(e.target.value)}
            placeholder="John Doe"
            className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-400"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Card Number (last 4 digits)</label>
          <input
            type="text"
            required
            value={cardLast4}
            onChange={e => setCardLast4(e.target.value.slice(0, 4))}
            placeholder="1234"
            maxLength={4}
            className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-400"
          />
        </div>
        {error && <p className="text-red-600 text-sm bg-red-50 p-3 rounded-lg">{error}</p>}
        <button
          type="submit"
          disabled={loading}
          className="w-full bg-indigo-600 text-white py-3 rounded-lg font-semibold hover:bg-indigo-700 transition disabled:opacity-50"
        >
          {loading ? 'Processing...' : `Pay $${event.ticketPrice}`}
        </button>
      </form>
    </div>
  );
}
