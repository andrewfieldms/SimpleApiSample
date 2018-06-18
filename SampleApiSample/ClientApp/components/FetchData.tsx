import * as React from 'react';
import axios from "axios";
import { RouteComponentProps } from 'react-router';

interface SearchResults {
    count: number;
    total: number;
    results: Video[];
}

interface Video {
    id: string;
    title: string;
    duration: string;
    url: string;
    reach: number;
}

interface SimpleApiSampleState {
    loading: boolean,
    searchResults: SearchResults;
} 

export class FetchData extends React.Component<RouteComponentProps<{}>, SimpleApiSampleState> {

    constructor() {
        super();
        this.state = {
            loading: true,
            searchResults: { count: 0, total: 0, results: [] }
        };

        this.loadData();
    }

    public async loadData() {
        let maxResults = 100;
        let searchResults = this.state.searchResults;
        let pageSize = 10;
        let pageCnt = 1;
        let minDuration = 5;
        let loading = true;

        while (true) {
            let data = await axios.get<SearchResults>("api/SampleData/GetVideos/?pageSize=" + pageSize + "&pageNum=" + pageCnt + "&minDuration=" + minDuration);

            searchResults.count += data.data.count;
            searchResults.total = data.data.total;

            data.data.results.forEach(item => {
                if (searchResults.results.length < maxResults) {
                    searchResults.results.push(item);
                }
            });

            if (this.state.searchResults.total === 0 ||
                this.state.searchResults.count === this.state.searchResults.total ||
                searchResults.results.length >= maxResults) {

                loading = false;
            }

            this.setState({ loading: loading, searchResults: searchResults });

            if (data.data.count > 0) {
                this.loadReachForSet(this.state.searchResults,
                    this.state.searchResults.results.length - data.data.count,
                    this.state.searchResults.results.length - 1);
            }

            if (!loading) {
                break;
            }
            pageCnt++;
        }

        // this.loadReach(searchResults);
    }

    public loadReachForSet(data: SearchResults, from: number, to: number) {
        let dataset: number[] = [];
        for (let t = from; t <= to; t++) {
            dataset.push(t);
        }

        this.loadReachThread(data, dataset);
    }

    public loadReach(data: SearchResults) {
        let threads = 10;
        let dataSets: number[][] = [];

        for (let t = 0; t < threads; t++) {
            let index = dataSets.push([]) - 1;

            for (let i = t; i < data.results.length; i += threads) {
                dataSets[index].push(i);
            }

            this.loadReachThread(data, dataSets[index]);
        }   
    }

    public async loadReachThread(data: SearchResults, indexesToLoad: number[]) {
        return new Promise<void>(async () => {
            for (let i = 0; i < indexesToLoad.length; i++) {
                let index = indexesToLoad[i];
                let reach = await axios.get<number>("api/SampleData/GetVideoReach/?id=" + data.results[index].id);
                data.results[index].reach = reach.data;
                this.setState({ searchResults: data });
            };
        });
    }

    public render() {
        let contents = this.state.searchResults.results.length == 0
            ? <p><em>Loading...</em></p>
            : this.renderVideos();

        return <div>
            <h1>Videos</h1>
            { contents }
        </div>;
    }

    private renderVideos() {
        return <div>
            <h4> {this.state.loading && <span>Loading... </span>} {this.state.searchResults.results.length} results found</h4>
            <h4>{this.state.searchResults.results.filter(vid => vid.reach > 0).length} of {this.state.searchResults.results.length} reach count loaded</h4>
            <table className='table'>
                <thead>
                    <tr>
                        <th>Title</th>
                        <th>Duration</th>
                        <th>Reach</th>
                    </tr>
                </thead>
                <tbody>
                    {this.state.searchResults.results.map(video =>
                        <tr key={video.id}>
                            <td><a href={video.url}>{video.title}</a></td>
                            <td>{video.duration}</td>
                            <td>{video.reach === 0 ? "..." : video.reach}</td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>;
    }
}
