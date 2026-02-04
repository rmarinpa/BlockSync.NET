import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { Dashboard } from './pages/Dashboard';
import { Ledger } from './pages/Ledger';
import { Hashes } from './pages/Hashes';
import { Actions } from './pages/Actions';
import { Diagnostics } from './pages/Diagnostics';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Layout />}>
          <Route index element={<Dashboard />} />
          <Route path="ledger" element={<Ledger />} />
          <Route path="hashes" element={<Hashes />} />
          <Route path="actions" element={<Actions />} />
          <Route path="diagnostics" element={<Diagnostics />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
