import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export function Navbar() {
  const { user, logout, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <nav className="bg-indigo-700 text-white px-6 py-4 flex items-center justify-between shadow-md">
      <Link to="/" className="text-xl font-bold tracking-wide">TicketBooking</Link>
      <div className="flex items-center gap-6 text-sm font-medium">
        <Link to="/events" className="hover:text-indigo-200 transition">Events</Link>
        {isAuthenticated ? (
          <>
            <Link to="/my-bookings" className="hover:text-indigo-200 transition">My Bookings</Link>
            <span className="text-indigo-300">Hi, {user?.firstName}</span>
            <button onClick={handleLogout} className="bg-indigo-500 hover:bg-indigo-400 px-3 py-1 rounded transition">
              Logout
            </button>
          </>
        ) : (
          <>
            <Link to="/login" className="hover:text-indigo-200 transition">Login</Link>
            <Link to="/register" className="bg-indigo-500 hover:bg-indigo-400 px-3 py-1 rounded transition">Register</Link>
          </>
        )}
      </div>
    </nav>
  );
}
