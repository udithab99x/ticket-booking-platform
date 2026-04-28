import { Link } from 'react-router-dom';
import type { Event } from '../types';

export function EventCard({ event }: { event: Event }) {
  const date = new Date(event.eventDate).toLocaleDateString('en-US', {
    weekday: 'short', year: 'numeric', month: 'short', day: 'numeric',
  });

  return (
    <div className="bg-white rounded-xl shadow hover:shadow-lg transition overflow-hidden flex flex-col">
      {event.imageUrl ? (
        <img src={event.imageUrl} alt={event.name} className="h-40 w-full object-cover" />
      ) : (
        <div className="h-40 bg-gradient-to-br from-indigo-400 to-purple-500 flex items-center justify-center text-white text-4xl font-bold">
          {event.name[0]}
        </div>
      )}
      <div className="p-4 flex flex-col gap-2 flex-1">
        <span className="text-xs font-semibold uppercase tracking-wide text-indigo-600 bg-indigo-50 px-2 py-0.5 rounded w-fit">
          {event.category}
        </span>
        <h3 className="font-bold text-gray-900 text-lg leading-tight">{event.name}</h3>
        <p className="text-sm text-gray-500">{event.venue}, {event.city}</p>
        <p className="text-sm text-gray-400">{date}</p>
        <div className="mt-auto flex items-center justify-between pt-3 border-t border-gray-100">
          <span className="text-indigo-700 font-bold text-lg">${event.ticketPrice}</span>
          <span className="text-xs text-gray-400">{event.availableSeats} seats left</span>
        </div>
        <Link
          to={`/events/${event.id}`}
          className="mt-2 block text-center bg-indigo-600 text-white py-2 rounded-lg hover:bg-indigo-700 transition text-sm font-medium"
        >
          View Details
        </Link>
      </div>
    </div>
  );
}
