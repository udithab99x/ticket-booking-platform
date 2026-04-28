import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '../lib/api';
import type { ApiResponse, Booking } from '../types';

const STATUS_COLORS: Record<string, string> = {
  Confirmed: 'bg-green-100 text-green-800',
  Pending: 'bg-yellow-100 text-yellow-800',
  Cancelled: 'bg-gray-100 text-gray-500',
  Failed: 'bg-red-100 text-red-700',
};

export function MyBookings() {
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['my-bookings'],
    queryFn: () => api.get<ApiResponse<Booking[]>>('/api/bookings/my').then(r => r.data),
  });

  const cancelMutation = useMutation({
    mutationFn: (id: string) => api.post(`/api/bookings/${id}/cancel`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['my-bookings'] }),
  });

  const bookings = data?.data ?? [];

  if (isLoading) return <div className="text-center py-20 text-gray-400">Loading your bookings...</div>;

  return (
    <div className="max-w-4xl mx-auto px-6 py-10">
      <h1 className="text-3xl font-bold text-gray-900 mb-6">My Bookings</h1>
      {bookings.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <p className="text-lg">No bookings yet.</p>
          <a href="/events" className="mt-4 inline-block text-indigo-600 hover:underline">Browse Events →</a>
        </div>
      ) : (
        <div className="space-y-4">
          {bookings.map(b => (
            <div key={b.id} className="bg-white rounded-xl border border-gray-200 p-5 flex items-center justify-between shadow-sm">
              <div className="space-y-1">
                <h3 className="font-bold text-gray-900">{b.eventName}</h3>
                <p className="text-sm text-gray-500">{new Date(b.eventDate).toLocaleDateString('en-US', { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' })}</p>
                <p className="text-sm text-gray-600">Seat: <span className="font-medium">{b.seatNumber}</span></p>
                <p className="text-sm font-bold text-indigo-700">${b.amount}</p>
              </div>
              <div className="flex flex-col items-end gap-3">
                <span className={`text-xs font-semibold px-3 py-1 rounded-full ${STATUS_COLORS[b.status] ?? 'bg-gray-100 text-gray-500'}`}>
                  {b.status}
                </span>
                {(b.status === 'Pending' || b.status === 'Confirmed') && (
                  <button
                    onClick={() => cancelMutation.mutate(b.id)}
                    disabled={cancelMutation.isPending}
                    className="text-xs text-red-500 hover:text-red-700 border border-red-200 px-3 py-1 rounded hover:bg-red-50 transition"
                  >
                    Cancel
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
