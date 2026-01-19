import { Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';

// Content pages
import HomePage from './content/index.mdx';
import GettingStartedPage from './content/getting-started/index.mdx';
import InstallationPage from './content/getting-started/installation.mdx';
import TroubleshootingPage from './content/getting-started/troubleshooting.mdx';

// Features (UI)
import FeaturesOverviewPage from './content/features/index.mdx';
import HudPage from './content/features/hud.mdx';
import CharacterMenuPage from './content/features/character-menu.mdx';
import DataFlowPage from './content/features/data-flow.mdx';

// Reference
import ConfigReferencePage from './content/reference/config.mdx';
import ChangelogPage from './content/reference/changelog.mdx';

// Tools
import VDebugPage from './content/tools/vdebug.mdx';
import DesignMockPage from './content/tools/design-mock.mdx';

// Contributing
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

        {/* Features (UI) */}
        <Route path="/features" element={<FeaturesOverviewPage />} />
        <Route path="/features/hud-bars" element={<HudPage />} />
        <Route path="/features/character-menu" element={<CharacterMenuPage />} />
        <Route path="/features/quest-tracker" element={<DataFlowPage />} />
        <Route path="/features/familiars" element={<CharacterMenuPage />} />

        {/* Reference */}
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
