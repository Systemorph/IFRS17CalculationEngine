<!--- 
<p align="right">
<img width="250" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/Systemorph_logo.png" alt="Systemorph logo">
</p> 
--->

<!--- 
The aim of this project is to apply the Systemorph IFRS 17 Calculation Engine to valuate insurance contracts
through the Analysis of Change of the Present Values.
Starting from the economic and insurance inputs, i.e. yield curves and nominal cash flows,
the Present Values are calculated and displayed so that the change from opening to closing balance can be
broken down and analyzed step by step.
--->

Together with the present project, we produced a series consisting of 3 episodes tackling the following topics:
1. The Theory Behind Economic Accounting,
2. The Systemorph Calculation Engine in Action, and
3. How to Customize the Input to your Dataset and Contracts.

Given that this project and video series specializes on Present Values and Analysis of Change approach,
the full IFRS 17 solution is much broader and provides a lot more functionalities than those presented
in this project. We invite you to clone the
[Full IFRS 17 Template Project](https://portal.stage.systemorph.cloud/project/present-value-analysis/env/dev/)
in order to test the full end-to-end solution.
You can reuse the data you prepared for this project in the Full Template project as well.

Additionally, if you are interested in the implemented methodologies and your keen to learn from
our open-source IFRS 17 code and documentation, please refer to the
[Systemorph Calculation Engine](https://portal.stage.systemorph.cloud/project/present-value-analysis/env/dev/). 


# The Theory Behind Economic Accounting 

The content of this section is presented in the first
[episode](https://systemorph-my.sharepoint.com/personal/amuolo_systemorph_com/)
of this series

<p align="center">
<img width="350" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/SM-YoutubePreview-S01E01.png" alt="Overview">
</p>

Computing the Present Value of the insurance contracts is the main goal of the IFRS 17 economic accounting standard.
Briefly, this is the fair price one would pay for all these insurance policies today.

For computing Present Values, the insurance contracts are allocated to homogeneous groups usually formed by type,
line of business, annual cohort to simplify reporting without a distinct loss of accuracy.
The Present Value is computed for each group of insurance contracts individually.
This requires that all the cash flows are estimated and modelled until the product is run off.
These are the amounts of cash and cash equivalents being transferred into and out of a business,
such as premium incomes, benefits, claims, and expenses.
Because the growth of the market and the inflation, the time of the expected cash flow impacts
the Present Value of that cash flow.
The timespan from now until the time of the cash flow is called maturity.

<p align="center">
<img width="850" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/overview.png" alt="Overview">
</p>

The Present Value of a cash flow is computed as the discounted cash flow
and can be expressed through the following formula:

$$ \text{PresentValue} = \displaystyle\frac{\text{CashFlow}}{(1+\text{InterestRate})^{\text{MaturityYears} }} ~.$$

The total Present Value of the policy is then the sum of Present Values of all cash flows.

Every period (Typically every quarter) an insurance company will need to prepare a statement
of the Present Value of future cash flows.
This statement is based on the value of a group of insurance contracts at the beginning
of the period, its development throughout the period, and the value at the end of the period.
Different effects contribute to the change in value between the beginning and the end of a period
and these effects are shown in the Analysis of Change.

<p align="center">
<img width="550" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/waterfallChart.png" alt="WaterfallChart">
</p>

In this analysis, we identify different components and their effects on the Present Value in the form of deltas, such as
 - Model correction for existing business,
 - Actual cash flows (which may differ from prior expectation),
 - Interest accretion, where the interest on different amounts manifests itself over the period,
 - Experience variance, new assessment by actuaries given newest developments,
 - Assumption updates, e.g., a new mortality table,
 - Financial assumption updates, e.g., a new yield curve,
 - Combined liabilities, a final run combining in-force and new business incl dependencies.


# On the Calculation and Reporting

The content of this section refers to the second
[episode](https://systemorph-my.sharepoint.com/personal/amuolo_systemorph_com/).

<p align="center">
<img width="350" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/SM-YoutubePreview-S01E02.png" alt="Overview">
</p>

This episode aims at demonstrating the use of the IFRS 17 calculation engine for computing Present Values
using the standard IFRS 17 Calculation Engine and the notebook "PresentValues - Episode 2".
Vanessa and Andrea will guide you through this journey.

<p align="center">
<img width="90%" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/VanessaAndrea.png" alt="VanessaAndrea">
</p>

The interest and discount rates can be derived by the assumed yield curve, which is stored
in the file "YieldCurve.xlsx", and imported in the notebook as the economic input.
Conversely, the values of the modelled cash flows are stored in the file "Cashflows.xlsx"
forming the insurance input.
From both inputs and thanks to the methods provided by the Systemorph IFRS 17 Calculation Engine,
it is possible to discount and comulate the cash flows, from which deltas can be computed and reported
per each step of the Analysis of Change.

<p align="center">
<img width="70%" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/flowchart.png" alt="flowchart">
</p>

In the last section of the notebook these steps are taken individually for a selected
Analysis of Change Step, so that the calculation can be checked. 


# How To Customize The Input: Do It Yourself

The content of this section refers to the third 
and last [episode](https://systemorph-my.sharepoint.com/personal/amuolo_systemorph_com/) of this series.

<p align="center">
<img width="350" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/SM-YoutubePreview-S01E03.png" alt="Overview">
</p>

The aim is to present how to customize the setup described in the Episode 2 to your business data set and company.
The corresponding notebook is named "PresentValues - Episode 3".

Firstly, the case of a company with many legal entities is covered, so that group of contracts can be defined
for the (e.g.) Swiss and German reporting nodes separately. 
This use case is discussed with examples of dedicated cash flows imported for the two groups,
together with the reports of the corresponding Present Values.
so the corresponding Present Values can be reported.
Lastly, the episode describes how to add a custom Analysis of Change step to the list. 
This task can be achieved simply by adding the entry in the AocType tab of the "Dimensions.xlsx" file. 
Automatic configuration are applied to this step in order to allow users to start importing 
cash flows for this freshly created step effortlessly.


# Got Questions

Meet our exceptional [Community Team](https://github.com/Systemorph/youplusifrs17/projects/1).
For support around the IFRS 17 CalculationEngine get in contact with our
[Community](https://github.com/Systemorph/youplusifrs17/projects/1).


# Contributing

All work on the **Full IFRS 17 Template** happens directly on
[GitHub](https://github.com/Systemorph/IFRS17CalculationEngine).

This project adheres to overall
[General Terms & Conditions for Systemorph Cloud](https://github.com/Systemorph/youplusifrs17/projects/1).






