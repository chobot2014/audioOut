import * as React from 'react';
import { RouteComponentProps } from 'react-router-dom';

export default class Home extends React.Component<RouteComponentProps<{}>, {}> {
    public render() {
        return <div>
            <h1>Something from Home component</h1>            
        </div>;
    }
}
