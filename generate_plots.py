import json
import pandas as pd
import matplotlib
import matplotlib.pyplot as plt
import seaborn as sns
from pathlib import Path
from bisect import bisect_left


def load_data(
    problem_dir, algorithms, num_trials, targets_to_display, max_budget
):
    data = {"algorithm": [], "target": [], "runtime": []}
    for algo_name, algo_dir in algorithms.items():
        p = Path(f"./Output/{problem_dir}/{algo_dir}")
        for i in range(1, num_trials + 1):
            json_str = p.joinpath(f"trial-{i}.json").read_text()
            record = json.loads(json_str)
            hitting_times = [int(r) for r in record["hitting_times"]]
            targets = [int(t) for t in record["targets"]]
            hitting_times.sort()
            targets.sort()
            for t in targets_to_display:
                data["algorithm"].append(algo_name)
                data["target"].append(t)
                k = bisect_left(targets, t)
                if k < len(targets) and hitting_times[k] <= max_budget:
                    data["runtime"].append(hitting_times[k])
                else:
                    data["runtime"].append(max_budget)

    return pd.DataFrame(data)


def generate_plot(data, file_name):
    matplotlib.use("pgf")
    matplotlib.rcParams.update(
        {
            "pgf.texsystem": "pdflatex",
            "font.family": "serif",  # use serif/main font for text elements
            "text.usetex": True,  # use inline math for ticks
            "pgf.rcfonts": False,  # don't setup fonts from rc parameters
        }
    )
    fig = plt.figure()
    fig_width = 2.268
    fig_height = fig_width
    fig.set_size_inches(w=fig_width, h=fig_height)
    ax = fig.add_subplot()
    sns.lineplot(
        data=data,
        x="target",
        y="runtime",
        hue="algorithm",
        style="algorithm",
        estimator="median",
        errorbar=("pi", 50),
        markers=True,
        dashes=True,
        sort=True,
        ax=ax,
    )
    ax.legend(loc="upper left", frameon=False)
    ax.set_xlabel("Target")
    ax.set_ylabel("$1$-Penalized Runtime")
    ax.set_yscale("log")
    for pos in ["top", "bottom", "right", "left"]:
        ax.spines[pos].set_edgecolor("black")
    fig.savefig(f"{file_name}.pgf", bbox_inches="tight")
    plt.close(fig)


if __name__ == "__main__":
    sns.set()
    sns.set_context("paper")
    sns.set_style("whitegrid")
    sns.set_palette(sns.color_palette("Dark2"))
    algorithms = {
        r"\textit{partial-restart} cgEA": "cgEA",
        r"\textit{smart-restart} cGA": "cGA",        
    }
    num_trials = 100
    budget = 10000000
    ring_data = load_data(
        "IsingRing", algorithms, num_trials, list(range(50, 101, 2)), budget
    )
    torus_data = load_data(
        "IsingTorus",
        algorithms,
        num_trials,
        list(range(100, 201, 4)),
        budget,
    )
    mivs_data = load_data(
        "Mivs", algorithms, num_trials, list(range(20, 51, 2)), budget
    )
    print("Generating plot for IsingRing")
    generate_plot(ring_data, "IsingRing")
    print("Generating plot for IsingTorus")
    generate_plot(torus_data, "IsingTorus")
    print("Generating plot for Mivs")
    generate_plot(mivs_data, "Mivs")
    print("Done")
