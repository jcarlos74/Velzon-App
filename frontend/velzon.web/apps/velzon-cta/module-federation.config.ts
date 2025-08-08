import { ModuleFederationConfig } from '@nx/webpack';

const config: ModuleFederationConfig = {
  name: 'velzon-cta',
  exposes: {
    './Routes': 'apps/velzon-cta/src/app/remote-entry/entry.routes.ts'
  },
};

export default config;
