import React, { Component } from 'react';
import { Route, Routes } from 'react-router-dom';
import AppRoutes from './AppRoutes';
import { Layout } from './components/Layout';
import './custom.css';
import { FluentProvider, teamsLightTheme } from '@fluentui/react-components';

export default class App extends Component {
  static displayName = App.name;

  render() {
      return (
          <FluentProvider theme={teamsLightTheme}>
          <Layout>
            <Routes>
              {AppRoutes.map((route, index) => {
                const { element, ...rest } = route;
                return <Route key={index} {...rest} element={element} />;
              })}
            </Routes>
          </Layout>
        </FluentProvider>
    );
  }
}
