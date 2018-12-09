import './css/site.css';
import 'bootstrap';
import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { AppContainer } from 'react-hot-loader';
import { Provider } from 'react-redux';
import { ConnectedRouter } from 'react-router-redux';
import { createBrowserHistory } from 'history';
import configureStore from './configureStore';
import { ApplicationState }  from './store';
import * as RoutesModule from './routes';
let routes = RoutesModule.routes;


const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href')!;
const history = createBrowserHistory({ basename: baseUrl });
const initialState = (window as any).initialReduxState as ApplicationState;
const store = configureStore(history, initialState);



function renderApp() {    
    ReactDOM.render(
        <AppContainer>
            <Provider store={ store }>
                <ConnectedRouter history={ history } children={ routes } />
            </Provider>
        </AppContainer>,
        document.getElementById('react-app')
    );
}

renderApp();

if (module.hot) {
    module.hot.accept('./routes', () => {
        routes = require<typeof RoutesModule>('./routes').routes;
        renderApp();
    });
}
