import { Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';

// Content pages
import HomePage from './content/index.mdx';
import GettingStartedPage from './content/getting-started/index.mdx';
import InstallationPage from './content/getting-started/installation.mdx';
import TroubleshootingPage from './content/getting-started/troubleshooting.mdx';

import ServerOverviewPage from './content/server/index.mdx';
import LevelingPage from './content/server/leveling.mdx';
import ExpertisePage from './content/server/expertise.mdx';
import LegaciesPage from './content/server/legacies.mdx';
import ClassesPage from './content/server/classes.mdx';
import FamiliarsPage from './content/server/familiars.mdx';
import QuestsPage from './content/server/quests.mdx';
import ProfessionsPage from './content/server/professions.mdx';
import PrestigePage from './content/server/prestige.mdx';
import WorldBossPage from './content/server/world-boss.mdx';

import ClientOverviewPage from './content/client/index.mdx';
import HudPage from './content/client/hud.mdx';
import CharacterMenuPage from './content/client/character-menu.mdx';
import DataFlowPage from './content/client/data-flow.mdx';

import CommandsReferencePage from './content/reference/commands.mdx';
import ConfigReferencePage from './content/reference/config.mdx';
import ChangelogPage from './content/reference/changelog.mdx';

import VDebugPage from './content/tools/vdebug.mdx';
import DesignMockPage from './content/tools/design-mock.mdx';

import ContributingPage from './content/contributing/index.mdx';

function App() {
  return (
    <Layout>
      <Routes>
        {/* Home */}
        <Route path="/" element={<HomePage />} />

        {/* Getting Started */}
        <Route path="/getting-started" element={<GettingStartedPage />} />
        <Route path="/getting-started/installation" element={<InstallationPage />} />
        <Route path="/getting-started/troubleshooting" element={<TroubleshootingPage />} />

        {/* Server */}
        <Route path="/server" element={<ServerOverviewPage />} />
        <Route path="/server/leveling" element={<LevelingPage />} />
        <Route path="/server/expertise" element={<ExpertisePage />} />
        <Route path="/server/legacies" element={<LegaciesPage />} />
        <Route path="/server/classes" element={<ClassesPage />} />
        <Route path="/server/familiars" element={<FamiliarsPage />} />
        <Route path="/server/quests" element={<QuestsPage />} />
        <Route path="/server/professions" element={<ProfessionsPage />} />
        <Route path="/server/prestige" element={<PrestigePage />} />
        <Route path="/server/world-boss" element={<WorldBossPage />} />

        {/* Client */}
        <Route path="/client" element={<ClientOverviewPage />} />
        <Route path="/client/hud" element={<HudPage />} />
        <Route path="/client/character-menu" element={<CharacterMenuPage />} />
        <Route path="/client/data-flow" element={<DataFlowPage />} />

        {/* Reference */}
        <Route path="/reference/commands" element={<CommandsReferencePage />} />
        <Route path="/reference/config" element={<ConfigReferencePage />} />
        <Route path="/reference/changelog" element={<ChangelogPage />} />

        {/* Tools */}
        <Route path="/tools/vdebug" element={<VDebugPage />} />
        <Route path="/tools/design-mock" element={<DesignMockPage />} />

        {/* Contributing */}
        <Route path="/contributing" element={<ContributingPage />} />

        {/* Fallback */}
        <Route path="*" element={<HomePage />} />
      </Routes>
    </Layout>
  );
}

export default App;
