/**
 * Documentation site navigation configuration
 * Single source of truth for sidebar and routing
 */

export interface NavItem {
  title: string;
  path?: string;
  children?: NavItem[];
  badge?: string;
}

export interface DocsConfig {
  title: string;
  description: string;
  repo: string;
  links: {
    thunderstoreClient: string;
    thunderstoreServer: string;
    github: string;
  };
  nav: NavItem[];
}

export const docsConfig: DocsConfig = {
  title: 'BloodCraftPlus',
  description: 'The Ultimate V Rising RPG Experience',
  repo: 'DiNaSoR/BloodCraftPlus',
  links: {
    thunderstoreClient: 'https://new.thunderstore.io/c/v-rising/p/DiNaSoR/EclipsePlus/',
    thunderstoreServer: 'https://new.thunderstore.io/c/v-rising/p/DiNaSoR/Bloodcraftplus/',
    github: 'https://github.com/DiNaSoR/BloodCraftPlus',
  },
  nav: [
    {
      title: 'Home',
      path: '/',
    },
    {
      title: 'Getting Started',
      path: '/getting-started',
      children: [
        { title: 'Installation', path: '/getting-started/installation' },
        { title: 'Troubleshooting', path: '/getting-started/troubleshooting' },
      ],
    },
    {
      title: 'Server (Bloodcraftplus)',
      path: '/server',
      children: [
        { title: 'Leveling System', path: '/server/leveling' },
        { title: 'Weapon Expertise', path: '/server/expertise' },
        { title: 'Blood Legacies', path: '/server/legacies' },
        { title: 'Classes', path: '/server/classes' },
        { title: 'Familiars', path: '/server/familiars' },
        { title: 'Quests', path: '/server/quests' },
        { title: 'Professions', path: '/server/professions' },
        { title: 'Prestige & Exoform', path: '/server/prestige' },
        { title: 'World Boss', path: '/server/world-boss', badge: 'NEW' },
      ],
    },
    {
      title: 'Client (EclipsePlus)',
      path: '/client',
      children: [
        { title: 'HUD Components', path: '/client/hud' },
        { title: 'Character Menu', path: '/client/character-menu' },
        { title: 'Data Flow', path: '/client/data-flow' },
      ],
    },
    {
      title: 'Reference',
      children: [
        { title: 'Chat Commands', path: '/reference/commands' },
        { title: 'Configuration', path: '/reference/config' },
        { title: 'Changelog', path: '/reference/changelog' },
      ],
    },
    {
      title: 'Tools',
      children: [
        { title: 'VDebug', path: '/tools/vdebug' },
        { title: 'Design Mock', path: '/tools/design-mock' },
      ],
    },
    {
      title: 'Contributing',
      path: '/contributing',
    },
  ],
};
