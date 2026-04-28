import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import api from '../lib/api';
import { useAuth } from '../context/AuthContext';
import type { ApiResponse, Event, Seat } from '../types';

export function EventDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const [selectedSeat, setSelectedSeat] = useState<Seat | null>(null);

  const { data: eventData } = useQuery({
    queryKey: ['event', id],
    queryFn: () => api.get<ApiResponse<Event>>(`/api/events/${id}`).then(r => r.data),
  });

  const { data: seatsData } = useQuery({
    queryKey: ['event-seats', id],
    queryFn: () => api.get<ApiResponse<Seat[]>>(`/api/events/${id}/seats`).then(r => r.data),
  });

  const event = eventData?.data;
  const seats = seatsData?.data ?? [];

  const handleBook = () => {
    if (!isAuthenticated) { navigate('/login'); return; }
    if (!selectedSeat) return;
    navigate(`/book/${id}`, { state: { seat: selectedSeat, event } });
  };

  if (!event) return <div className="text-center py-20 text-gray-400">Loading...</div>;

  const date = new Date(event.eventDate).toLocaleString('en-US', {
    weekday: 'long', year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit',
  });

  // Group seats by row
  const rows = seats.reduce((acc, s) => {
    if (!acc[s.row]) acc[s.row] = [];
    acc[s.row].push(s);
    return acc;
  }, {} as Record<string, Seat[]>);

  return (
    <div className="max-w-5xl mx-auto px-6 py-10">
      <div className="grid md:grid-cols-2 gap-8 mb-10">
        {/* Info */}
        <div>
          <span className="text-xs font-semibold uppercase text-indigo-600 bg-indigo-50 px-2 py-1 rounded">{event.category}</span>
          <h1 className="text-3xl font-bold text-gray-900 mt-2 mb-2">{event.name}</h1>
          <p className="text-gray-600 mb-4">{event.description}</p>
          <div className="space-y-2 text-sm text-gray-700">
            <p><span className="font-medium">Venue:</span> {event.venue}, {event.city}</p>
            <p><span className="font-medium">Date:</span> {date}</p>
            <p><span className="font-medium">Available:</span> {event.availableSeats} / {event.totalSeats} seats</p>
            <p><span className="font-medium">Price:</span> <span className="text-2xl font-bold text-indigo-700">${event.ticketPrice}</span> per ticket</p>
          </div>
        </div>

        {/* Book Panel */}
        <div className="bg-gray-50 rounded-xl p-6 border border-gray-200">
          <h2 className="font-bold text-lg text-gray-800 mb-4">Select a Seat</h2>
          {selectedSeat && (
            <div className="mb-4 p-3 bg-indigo-50 rounded-lg border border-indigo-200 text-sm">
              Selected: <strong>Row {selectedSeat.row} — {selectedSeat.seatNumber}</strong>
            </div>
          )}
          <button
            onClick={handleBook}
            disabled={!selectedSeat}
            className="w-full bg-indigo-600 text-white py-3 rounded-lg font-semibold hover:bg-indigo-700 transition disabled:opacity-40 disabled:cursor-not-allowed"
          >
            {isAuthenticated ? 'Proceed to Payment' : 'Login to Book'}
          </button>
        </div>
      </div>

      {/* Seat Map */}
      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <h2 className="font-bold text-lg text-gray-800 mb-2">Seat Map</h2>
        <div className="flex gap-4 text-xs mb-4">
          <span className="flex items-center gap-1"><span className="w-4 h-4 bg-green-100 border border-green-400 rounded inline-block"></span>Available</span>
          <span className="flex items-center gap-1"><span className="w-4 h-4 bg-indigo-500 rounded inline-block"></span>Selected</span>
          <span className="flex items-center gap-1"><span className="w-4 h-4 bg-gray-200 border border-gray-300 rounded inline-block"></span>Booked</span>
        </div>
        <div className="space-y-2 max-h-80 overflow-y-auto">
          {Object.entries(rows).map(([row, rowSeats]) => (
            <div key={row} className="flex items-center gap-1">
              <span className="w-6 text-xs font-bold text-gray-400 text-right shrink-0">{row}</span>
              <div className="flex gap-1 flex-wrap">
                {rowSeats.map(seat => (
                  <button
                    key={seat.id}
                    disabled={seat.isBooked}
                    onClick={() => setSelectedSeat(seat)}
                    title={seat.seatNumber}
                    className={`w-7 h-7 text-xs rounded transition font-medium
                      ${seat.isBooked ? 'bg-gray-200 text-gray-400 cursor-not-allowed' :
                        selectedSeat?.id === seat.id ? 'bg-indigo-500 text-white' :
                        'bg-green-100 border border-green-400 text-green-700 hover:bg-green-200'}`}
                  >
                    {seat.seatNumber.replace(row, '')}
                  </button>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
