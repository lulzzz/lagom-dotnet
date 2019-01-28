import * as React from 'react';
import './App.css';

class App extends React.Component<any, any> {

  public constructor(props: any) {
    super(props);
    this.state = { data: { Children: [] } };
  }

  public componentDidMount() {
    fetch("http://localhost:5000/api/visualizer/list?path=" + this.props.data.Path + "/")
      .then(res => res.json())
      .then(json => {
        this.setState({ data: json });
      });
  }

  public render() {
    return (
      <ul>
        {
          !this.state.data ? null :
            this.state.data.Children.map(
              (child: any, i: number) => <li key={i}>{child.Name}<App data={child} /></li>
            )
        }
      </ul>
    );
  }

}

export default App;
