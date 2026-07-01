import {
  heroBars3,
  heroCheck,
  heroCog6Tooth,
  heroHome,
  heroPlus,
  heroXMark,
} from '@ng-icons/heroicons/outline';
import { hugeLogout01, hugeSearch01 } from '@ng-icons/huge-icons';
import { lucidePencil, lucideTrash2 } from '@ng-icons/lucide';

/** Semantic icon keys mapped to registered ng-icon names. */
export const APP_ICONS = {
  navHome: 'heroHome',
  navMachine: 'heroCog6Tooth',
  menu: 'heroBars3',
  close: 'heroXMark',
  success: 'heroCheck',
  add: 'heroPlus',
  search: 'hugeSearch01',
  edit: 'lucidePencil',
  delete: 'lucideTrash2',
  logout: 'hugeLogout01',
} as const;

export type AppIconName = (typeof APP_ICONS)[keyof typeof APP_ICONS];

/** Icons registered once via `provideIcons()` in app config. */
export const APP_ICON_REGISTRY = {
  heroHome,
  heroCog6Tooth,
  heroBars3,
  heroXMark,
  heroCheck,
  heroPlus,
  hugeSearch01,
  hugeLogout01,
  lucidePencil,
  lucideTrash2,
};
