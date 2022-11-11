<!--- 
<p align="right">
<img width="250" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/Systemorph_logo.png" alt="Systemorph logo">
</p> 
--->

The aim of this project is to apply the Systemorph Ifrs 17 Calculation Engine to valuate insurance contracts 
through the Analysis of Change of the Present Values.
Starting from the economic and insurance inputs, i.e. yield curves and nominal cash flows, respectively, 
the Present Values are calculated and displayed so that the change from opening to closing balance can be 
broken down and analyzed step by step.

Together with the present project, we produced a series consisting of 3 episodes tackling: 
1. the theory of Present Value and Analysis of Change approach, 
2. the Systemorph Calculation Engine in action, and 
3. how to customize the input to your dataset and contracts.

The full IFRS 17 solution is much broader and provides a lot more functionalities than those presented in this project. 
We invite you to clone the 
[Full IFRS 17 Template Project](https://portal.stage.systemorph.cloud/project/present-value-analysis/env/dev/)
in order to test the full end-to-end solution. 
You can reuse the data you prepared for this project in the Full Template project as well.

Additionally, if you are interested in the implemented methodologies and your keen to learn from our open-source IFRS 17 
code and documentation, please refer to the
[Systemorph Calculation Engine](https://portal.stage.systemorph.cloud/project/present-value-analysis/env/dev/). 


# The Theory Behind Economic Accounting 

The content of this section is presented in the first [episode](https://systemorph-my.sharepoint.com/personal/amuolo_systemorph_com/)
of this series

<p align="center">
<img width="350" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/SM-youtube-preview-S01E01.png" alt="Overview">
</p>

<p align="center">
<img width="850" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/overview.png" alt="Overview">
</p>

Computing the Present Value of the insurance contracts is the main goal of the IFRS 17 economic accounting standard.
Briefly, this is the fair price one would pay for all these insurance policies today. 

For computing present values, the insurance contracts are allocated to homogeneous groups usually formed by type, line of business, 
annual cohort to simplify reporting without a distinct loss of accuracy.
We will compute the present value for each group of insurance contracts individually.
This requires that all the cash flows are estimated and modelled until the product is run off.
These are the amounts of cash and cash equivalents being transferred into and out of a business, such as premium incomes, benefits ...

<p align="center">
<img width="550" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/waterfallChart.png" alt="WaterfallChart">
</p>

Because the growth of the market and the inflation, the time of the expected cash flow impacts the present value of that cash flow.
The timespan from now until the time of the cash flow is called maturity.
Therefore the present value of a cash flow is computed as the discounted cash flow and can be expressed through the following formula:

$$ \text{PresentValue} = \displaystyle\frac{\text{CashFlow}}{(1+\text{InterestRate})^{\text{MaturityYears}   }}.$$

The total present value of the policy is then the sum of present values of all cash flows.

Every period (Typically every quarter) an insurance company will need to prepare a statement of the present value of future cash flows. 
This statement is based on the value of a group of insurance contracts at the beginning of the period, its development throughout the period, and the value at the end of the period.
Different effects contribute to the change in value between the beginning and the end of a period and these effects are shown in the Analysis of Change.

In this analysis, we identify different components and their effects on the Present Value in the form of deltas, such as
 - Model correction for existing business, 
 - Actual cash flows (which may differ from prior expectation),
 - Interest accretion, where the interest on different amounts manifests itself over the period, 
 - Experience variance, new assessment by actuaries given newest developments,
 - Assumption updates, e.g., a new mortality table,
 - Financial assumption updates, e.g., a new yield curve,
 - Combined liabilities, a final run combining in-force and new business incl dependencies.

# On the Calculation and Reporting

The content of this section refers to the second [episode](https://systemorph-my.sharepoint.com/personal/amuolo_systemorph_com/). 

<p align="center">
<img width="350" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/SM-youtube-preview-S01E02.png" alt="Overview">
</p>

<p align="center">
<img width="90%" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/VanessaAndrea.png" alt="VanessaAndrea">
</p>

This episode aims at demonstrating the use of the IFRS 17 calculation engine for computing Present Values
using the standard IFRS 17 Calculation Engine.
The values of the analyzed cash flows are stored in "Cashflows.xlsx," together with their description,
which are used by the computational notebook "PresentValues - Episode 2."
Furthermore the notebook uses the information about the interest rate stored in "YieldCurve.xlsx."
With this information, with few commands the calculation engine displays the wanted present values separated per each analysis of change step.

In the notebook further on, the computation of the present values is broken down and accompanied with charts of interest rates and cash flows.

<p align="center">
<img width="70%" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/flowchart.png" alt="flowchart">
</p>

# How To Customize The Input: Do It Yourself

The content of this section refers to the third 
and last [episode](https://systemorph-my.sharepoint.com/personal/amuolo_systemorph_com/) of this series.

<p align="center">
<img width="350" src="https://portal.stage.systemorph.cloud/api/project/present-value-analysis/env/dev/file/download?path=Images/SM-youtube-preview-S01E03.png" alt="Overview">
</p>

The aim is to present how to adapt the tools described in Episode 2 to the custom data set.
For that reason, three more spreadsheets representing cash flows are added,
along with two spreadsheets containing data about reporting nodes. 
The corresponding notebook "PresentValues - Episode 3" differs from the previous one 
only in the additional commands regarding importing the additional cash flows.
For all the changes please see the video.

# Meet our Community Team

Link to Community Team Landing Page. 

Link to Git Hub Page.
