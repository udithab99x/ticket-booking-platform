import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import api from '../lib/api';
import { EventCard } from '../components/EventCard';
import type { ApiResponse, PagedResponse, Event } from '../types';

export function Home() {
  const { data } = useQuery({
    queryKey: ['events-featured'],
    queryFn: () => api.get<ApiResponse<PagedResponse<Event>>>('/api/events?pageSize=6').then(r => r.data),
  });

  const events = data?.data?.items ?? [];

  return (
    <div>
      {/* Hero */}
      <section className="bg-gradient-to-br from-indigo-700 to-purple-700 text-white py-20 px-6 text-center">
        <h1 className="text-4xl md:text-5xl font-extrabold mb-4">Find & Book Amazing Events</h1>
        <p className="text-indigo-200 text-lg mb-8 max-w-xl mx-auto">
          Concerts, sports, festivals, theatre — get your tickets in seconds.
        </p>
        <Link to="/events" className="bg-white text-indigo-700 font-bold px-8 py-3 rounded-full hover:bg-indigo-50 transition">
          Browse Events
        </Link>
      </section>

      {/* Featured Events */}
      <section className="max-w-6xl mx-auto px-6 py-12">
        <h2 className="text-2xl font-bold text-gray-900 mb-6">Featured Events</h2>
        {events.length === 0 ? (
          <p className="text-gray-400 text-center py-12">No events available yet.</p>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
            {events.map(e => <EventCard key={e.id} event={e} />)}
          </div>
        )}
        <div className="text-center mt-8">
          <Link to="/events" className="text-indigo-600 font-semibold hover:underline">See all events →</Link>
        </div>
      </section>
    </div>
  );
}
