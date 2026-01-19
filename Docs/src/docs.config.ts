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
    thunderstore: string;
    github: string;
    bloodcraft: string;
  };
  nav: NavItem[];
}

export const docsConfig: DocsConfig = {
  title: 'BloodUI',
  description: 'Client UI Companion for BloodCraft',
  repo: 'DiNaSoR/BloodUI',
  links: {
    thunderstore: 'https://thunderstore.io/c/v-rising/p/DiNaSoR/BloodUI/',
    github: 'https://github.com/DiNaSoR/BloodUI',
    bloodcraft: 'https://thunderstore.io/c/v-rising/p/zfolmt/Bloodcraft/',
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
        { title: 'Configuration', path: '/getting-started/configuration' },
        { title: 'Troubleshooting', path: '/getting-started/troubleshooting' },
      ],
    },
    {
      title: 'UI Features',
      path: '/features',
      children: [
        { title: 'HUD Bars', path: '/features/hud-bars' },
        { title: 'Character Menu', path: '/features/character-menu' },
        { title: 'Quest Tracker', path: '/features/quest-tracker' },
        { title: 'Familiar Panel', path: '/features/familiars' },
      ],
    },
    {
      title: 'Reference',
      children: [
        { title: 'Configuration', path: '/reference/config' },
        { title: 'Changelog', path: '/reference/changelog' },
      ],
    },
    {
      title: 'Contributing',
      path: '/contributing',
    },
  ],
};
